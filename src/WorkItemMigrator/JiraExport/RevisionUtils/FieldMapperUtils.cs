using Atlassian.Jira;
using Common.Config;
using JiraExport.RevisionUtils;
using Migration.Common;
using Migration.Common.Config;
using Migration.Common.Log;
using Migration.WIContract;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace JiraExport
{
    public static class FieldMapperUtils
    {
        public static object MapRemainingWork(string seconds)
        {
            var secs = 0d;
            try
            {
                if (seconds == null)
                {
                    throw new FormatException();
                }
                secs = Convert.ToDouble(seconds);
            }
            catch (FormatException)
            {
                Logger.Log(LogLevel.Warning, $"A FormatException was thrown when converting RemainingWork value '{seconds}' to double. Defaulting to RemainingWork = null.");
                return null;
            }
            return TimeSpan.FromSeconds(secs).TotalHours;
        }

        public static (bool, object) MapTitle(JiraRevision r)
        {
            if (r == null)
                throw new ArgumentNullException(nameof(r));

            if (r.Fields.TryGetValue("summary", out object summary))
                return (true, $"[{r.ParentItem.Key}] {summary}");
            else
                return (false, null);
        }
        public static (bool, object) MapTitleWithoutKey(JiraRevision r)
        {
            if (r == null)
                throw new ArgumentNullException(nameof(r));

            if (r.Fields.TryGetValue("summary", out object summary))
                return (true, summary);
            else
                return (false, null);
        }

        public static (bool, object) MapValue(JiraRevision r, string itemSource, string itemTarget, ConfigJson config, ExportIssuesSummary exportIssuesSummary)
        {
            if (r == null)
                throw new ArgumentNullException(nameof(r));

            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var targetWit = (from t in config.TypeMap.Types where t.Source == r.Type select t.Target).FirstOrDefault();

            var hasFieldValue = r.Fields.TryGetValue(itemSource, out object value);

            if (!hasFieldValue)
                return (false, null);

            foreach (var item in config.FieldMap.Fields.Where(i => i.Mapping?.Values != null))
            {
                var sourceAndTargetMatch = item.Source == itemSource && item.Target == itemTarget;
                var forOrAllMatch = item.For.Contains(targetWit) || item.For == "All";  // matches "For": "All", or when this Wit is specifically named.
                var notForMatch = !string.IsNullOrWhiteSpace(item.NotFor) && !item.NotFor.Contains(targetWit);  // matches if not-for is specified and doesn't contain this Wit.

                if (sourceAndTargetMatch && (forOrAllMatch || notForMatch))
                {
                    if (value == null)
                    {
                        return (true, null);
                    }
                    var mappedValue = (from s in item.Mapping.Values where s.Source == value.ToString() select s.Target).FirstOrDefault();
                    if (string.IsNullOrEmpty(mappedValue))
                    {
                        Logger.Log(LogLevel.Warning, $"Missing mapping value '{value}' for field '{itemSource}' for item type '{r.Type}'.");
                        if(itemSource == "status")
                        {
                            exportIssuesSummary.AddUnmappedIssueState(r.Type, value.ToString());
                        }
                    }
                    return (true, mappedValue);
                }
            }
            return (true, value);
        }

        public static (bool, object) MapRenderedValue(JiraRevision r, string sourceField, bool isCustomField, string customFieldName, ConfigJson config)
        {
            if (r == null)
                throw new ArgumentNullException(nameof(r));

            if (config == null)
                throw new ArgumentNullException(nameof(config));

            sourceField = SetCustomFieldName(sourceField, isCustomField, customFieldName);

            var fieldName = sourceField + "$Rendered";

            var targetWit = (from t in config.TypeMap.Types where t.Source == r.Type select t.Target).FirstOrDefault();

            var hasFieldValue = r.Fields.TryGetValue(fieldName, out object value);
            if (!hasFieldValue)
                return (false, null);

            foreach (var item in config.FieldMap.Fields)
            {
                if (((item.Source == fieldName && (item.For.Contains(targetWit) || item.For == "All")) ||
                      item.Source == fieldName && (!string.IsNullOrWhiteSpace(item.NotFor) && !item.NotFor.Contains(targetWit))) &&
                      item.Mapping?.Values != null)
                {
                    var mappedValue = (from s in item.Mapping.Values where s.Source == value.ToString() select s.Target).FirstOrDefault();
                    if (string.IsNullOrEmpty(mappedValue))
                    {
                        Logger.Log(LogLevel.Warning, $"Missing mapping value '{value}' for field '{fieldName}'.");
                    }
                    return (true, mappedValue);
                }
            }
            value = CorrectRenderedHtmlvalue(value, r, config.IncludeJiraCssStyles);

            return (true, value);
        }



        public static object MapTags(string labels)
        {
            if (labels == null)
                throw new ArgumentNullException(nameof(labels));

            if (string.IsNullOrWhiteSpace(labels))
                return string.Empty;

            var tags = labels.Split(' ');
            if (!tags.Any())
                return string.Empty;
            else
                return string.Join(";", tags);
        }

        public static object MapArray(string field)
        {
            if (field == null)
                throw new ArgumentNullException(nameof(field));

            if (string.IsNullOrWhiteSpace(field))
                return null;

            var values = field.Split(',');
            if (!values.Any())
                return null;
            else
                return string.Join(";", values);
        }

        public static object MapSprint(string iterationPathsString)
        {
            if (string.IsNullOrWhiteSpace(iterationPathsString))
                return null;

            var iterationPaths = iterationPathsString.Split(',').AsEnumerable();
            iterationPaths = iterationPaths.Select(ip => ip.Trim());
            var iterationPath = iterationPaths.Last();

            iterationPath = ReplaceAzdoInvalidCharacters(iterationPath);

            return iterationPath;
        }

        /// <summary>
        /// Find the first custom field with the right name that has a value.
        /// </summary>
        /// <param name="fields"><see cref="JiraRevision.Fields">JiraRevision.Fields</see> dictionary.</param>
        /// <param name="customFieldIds">The IDs of the Custom Fields to search - more than one custom field can have the same name.</param>
        /// <param name="fieldValueObject">The value of the first field in the list that has a value.  Null if no values are found.</param>
        /// <returns>True if one of the identified custom fields contains a value.</returns>
        internal static bool TryGetFirstValue(this Dictionary<string,object> fields, List<string> customFieldIds, out object fieldValueObject)
        {
            foreach(var name in customFieldIds)
            {
                if (fields.TryGetValue(name, out fieldValueObject) && fieldValueObject != null)
                {
                    return true;
                }
            }

            fieldValueObject = null;
            return false;
        }

        private static readonly Dictionary<string, decimal> CalculatedLexoRanks = new Dictionary<string, decimal>();
        private static readonly Dictionary<decimal, string> CalculatedRanks = new Dictionary<decimal, string>();

        private static readonly Regex LexoRankRegex = new Regex(@"^[0-2]\|[0-9a-zA-Z]*(\:[0-9a-zA-Z]*)?$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);

        public static object MapLexoRank(string lexoRank)
        {
            if (string.IsNullOrEmpty(lexoRank) || !LexoRankRegex.IsMatch(lexoRank))
                return decimal.MaxValue;

            if (CalculatedLexoRanks.ContainsKey(lexoRank))
            {
                Logger.Log(LogLevel.Warning, "Duplicate rank detected. You may need to re-balance the JIRA LexoRank. see: https://confluence.atlassian.com/adminjiraserver/managing-lexorank-938847803.html");
                return CalculatedLexoRanks[lexoRank];
            }

            // split by bucket and sub-rank delimiters
            var lexoSplit = lexoRank.Split(new[] {'|', ':'}, StringSplitOptions.RemoveEmptyEntries);

            // calculate the numeric value of the rank and sub-rank (if available)
            var b36Rank = Base36.Decode(lexoSplit[1]);
            var b36SubRank = lexoSplit.Length == 3 && !string.IsNullOrEmpty(lexoSplit[2])
                ? Base36.Decode(lexoSplit[2])
                : 0L;

            // calculate final rank value
            var rank = Math.Round(
                Convert.ToDecimal($"{b36Rank}.{b36SubRank}", CultureInfo.InvariantCulture.NumberFormat),
                7 // DevOps seems to ignore anything over 7 decimal places long
            );

            if (CalculatedRanks.ContainsKey(rank) && CalculatedRanks[rank] != lexoRank)
            {
                Logger.Log(LogLevel.Warning, "Duplicate rank detected for different LexoRank values. You may need to re-balance the JIRA LexoRank. see: https://confluence.atlassian.com/adminjiraserver/managing-lexorank-938847803.html");
            }
            else
            {
                CalculatedRanks.Add(rank, lexoRank);
            }

            CalculatedLexoRanks.Add(lexoRank, rank);
            return rank;
        }

        public static string CorrectRenderedHtmlvalue(object value, JiraRevision revision, bool includeJiraStyle)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (revision == null)
                throw new ArgumentNullException(nameof(revision));

            var htmlValue = value.ToString();

            if (string.IsNullOrWhiteSpace(htmlValue))
                return htmlValue;

            foreach (var att in revision.AttachmentActions.Where(aa => aa.ChangeType == RevisionChangeType.Added).Select(aa => aa.Value))
            {
                if (!string.IsNullOrWhiteSpace(att.Url) && htmlValue.Contains(att.Url))
                    htmlValue = htmlValue.Replace(att.Url, att.Url);
            }

            htmlValue = RevisionUtility.ReplaceHtmlElements(htmlValue);

            if (includeJiraStyle)
            {
                string css = ReadEmbeddedFile("JiraExport.jirastyles.css");
                if (string.IsNullOrWhiteSpace(css))
                    Logger.Log(LogLevel.Warning, $"Could not read css styles for rendered field in {revision.OriginId}.");
                else
                    htmlValue = "<style>" + css + "</style>" + htmlValue;
            }

            return htmlValue;
        }

        private static string ReadEmbeddedFile(string resourceName)
        {
            var assembly = Assembly.GetEntryAssembly();

            try
            {
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (ArgumentNullException)
            {
                return "";
            }
        }
        private static string SetCustomFieldName(string sourceField, bool isCustomField, string customFieldName)
        {
            if (isCustomField)
            {
                sourceField = customFieldName;
            }

            return sourceField;
        }

        private static string ReplaceAzdoInvalidCharacters(string inputString)
        {
            return Regex.Replace(inputString, "[/$?*:\"&<>#%|+]", "", RegexOptions.None, TimeSpan.FromMilliseconds(100));
        }
    }

}