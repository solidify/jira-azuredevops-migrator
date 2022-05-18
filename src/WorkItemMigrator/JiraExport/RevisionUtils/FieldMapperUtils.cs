using Common.Config;
using Migration.Common;
using Migration.Common.Log;
using Migration.WIContract;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace JiraExport
{
    public static class FieldMapperUtils
    {
        public static object MapRemainingWork(string seconds)
        {
            var secs = Convert.ToDouble(seconds);
            return TimeSpan.FromSeconds(secs).TotalHours;
        }

        public static (bool, object) MapTitle(JiraRevision r)
        {
            if (r.Fields.TryGetValue("summary", out object summary))
                return (true, $"[{r.ParentItem.Key}] {summary}");
            else
                return (false, null);
        }
        public static (bool, object) MapTitleWithoutKey(JiraRevision r)
        {
            if (r.Fields.TryGetValue("summary", out object summary))
                return (true, summary);
            else
                return (false, null);
        }

        public static (bool, object) MapValue(JiraRevision r, string itemSource, ConfigJson config)
        {
            var targetWit = (from t in config.TypeMap.Types where t.Source == r.Type select t.Target).FirstOrDefault();

            if (r.Fields.TryGetValue(itemSource, out object value))
            {
                foreach (var item in config.FieldMap.Fields)
                {
                    if (((item.Source == itemSource && (item.For.Contains(targetWit) || item.For == "All")) ||
                          item.Source == itemSource && (!string.IsNullOrWhiteSpace(item.NotFor) && !item.NotFor.Contains(targetWit))) &&
                          item.Mapping?.Values != null)
                    {
                        var mappedValue = (from s in item.Mapping.Values where s.Source == value.ToString() select s.Target).FirstOrDefault();
                        if (string.IsNullOrEmpty(mappedValue))
                        {
                            Logger.Log(LogLevel.Warning, $"Missing mapping value '{value}' for field '{itemSource}' for item type '{r.Type}'.");
                        }
                        return (true, mappedValue);
                    }
                }
                return (true, value);
            }
            else
            {
                return (false, null);
            }
        }

        public static (bool, object) MapRenderedValue(JiraRevision r, string sourceField, bool isCustomField, string customFieldName, ConfigJson config)
        {
            if (isCustomField)
            {
                sourceField = customFieldName;
            }
            var fieldName = sourceField + "$Rendered";

            var targetWit = (from t in config.TypeMap.Types where t.Source == r.Type select t.Target).FirstOrDefault();

            if (r.Fields.TryGetValue(fieldName, out object value))
            {
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
                value = CorrectRenderedHtmlvalue(value, r);

                return (true, value);
            }
            else
            {
                return (false, null);
            }
        }

        public static object MapTags(string labels)
        {
            if (string.IsNullOrWhiteSpace(labels))
                return null;

            var tags = labels.Split(' ');
            if (!tags.Any())
                return null;
            else
                return string.Join(";", tags);
        }

        public static object MapArray(string field)
        {
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

            return iterationPath;
        }

        public static string CorrectRenderedHtmlvalue(object value, JiraRevision revision)
        {
            var htmlValue = value.ToString();

            if(String.IsNullOrWhiteSpace(htmlValue))
            {
                throw new ArgumentException(nameof(value));
            }

            foreach (var att in revision.AttachmentActions.Where(aa => aa.ChangeType == RevisionChangeType.Added).Select(aa => aa.Value))
            {
                if (!string.IsNullOrWhiteSpace(att.Url) && htmlValue.Contains(att.Url))
                    htmlValue = htmlValue.Replace(att.Url, att.Url);
            }

            htmlValue = RevisionUtility.ReplaceHtmlElements(htmlValue);

            string css = ReadEmbeddedFile("JiraExport.jirastyles.css");
            if (string.IsNullOrWhiteSpace(css))
                Logger.Log(LogLevel.Warning, $"Could not read css styles for rendered field in {revision.OriginId}.");
            else
                htmlValue = "<style>" + css + "</style>" + htmlValue;

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
            } catch (ArgumentNullException)
            {
                return "";
            }
        }
    }

}