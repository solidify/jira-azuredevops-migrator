﻿using System;
using System.Collections.Generic;
using System.Linq;

using Common.Config;

using Migration.Common;
using Migration.Common.Config;
using Migration.Common.Log;
using Migration.WIContract;

namespace JiraExport
{
    internal class JiraMapper : BaseMapper<JiraRevision>
    {
        private readonly JiraProvider _jiraProvider;
        private readonly Dictionary<string, FieldMapping<JiraRevision>> _fieldMappingsPerType;
        private readonly HashSet<string> _targetTypes;
        private readonly ConfigJson _config;

        public JiraMapper(JiraProvider jiraProvider, ConfigJson config) : base(jiraProvider?.Settings?.UserMappingFile)
        {
            _jiraProvider = jiraProvider;
            _config = config;
            _targetTypes = InitializeTypeMappings();
            _fieldMappingsPerType = InitializeFieldMappings();
        }

        /// <summary>
        /// Add or remove single link
        /// </summary>
        /// <param name="r"></param>
        /// <param name="links"></param>
        /// <param name="field"></param>
        /// <param name="type"></param>
        /// <returns>True if link is added, false if it's not</returns>
        private void AddRemoveSingleLink(JiraRevision r, List<WiLink> links, string field, string type)
        {
            if (r.Fields.TryGetValue(field, out object value))
            {
                var changeType = value == null ? ReferenceChangeType.Removed : ReferenceChangeType.Added;
                var linkType = (from t in _config.LinkMap.Links where t.Source == type select t.Target).FirstOrDefault();

                // regardless if action is add or remove, as there can be only one, we remove previous epic link if it exists
                if (r.Index != 0)
                {
                    var prevLinkValue = r.ParentItem.Revisions[r.Index - 1].GetFieldValue(field);
                    // if previous value is not null, add removal of previous link
                    if (!string.IsNullOrWhiteSpace(prevLinkValue))
                    {
                        var removeLink = new WiLink()
                        {
                            Change = ReferenceChangeType.Removed,
                            SourceOriginId = r.ParentItem.Key,
                            TargetOriginId = prevLinkValue,
                            WiType = linkType
                        };

                        links.Add(removeLink);
                    }
                }

                if (changeType == ReferenceChangeType.Added)
                {
                    string linkedItemKey = (string)value;

                    var link = new WiLink()
                    {
                        Change = changeType,
                        SourceOriginId = r.ParentItem.Key,
                        TargetOriginId = linkedItemKey,
                        WiType = linkType,
                    };

                    links.Add(link);
                }
            }
        }

        private List<string> GetWorkItemTypes(params string[] notFor)
        {
            List<string> list;
            if (notFor != null && notFor.Any())
            {
                list = _targetTypes.Where(t => !notFor.Contains(t)).ToList();
            }
            else
            {
                list = _targetTypes.ToList();
            }
            return list;
        }

        #region Mapping definitions

        private WiRevision MapRevision(JiraRevision r)
        {
            Logger.Log(LogLevel.Debug, $"Mapping revision {r.Index}.");

            List<WiAttachment> attachments = MapAttachments(r);
            List<WiField> fields = MapFields(r);
            List<WiLink> links = MapLinks(r);

            return new WiRevision()
            {
                ParentOriginId = r.ParentItem.Key,
                Index = r.Index,
                Time = r.Time,
                Author = MapUser(r.Author),
                Attachments = attachments,
                Fields = fields,
                Links = links,
                AttachmentReferences = attachments.Any()
            };
        }

        protected override string MapUser(string sourceUser)
        {
            if (string.IsNullOrWhiteSpace(sourceUser))
                return null;

            var email = _jiraProvider.GetUserEmail(sourceUser);
            return base.MapUser(email);
        }

        internal WiItem Map(JiraItem issue)
        {
            var wiItem = new WiItem();

            if (_config.TypeMap.Types != null)
            {
                var type = (from t in _config.TypeMap.Types where t.Source == issue.Type select t.Target).FirstOrDefault();

                if (type != null)
                {
                    var revisions = issue.Revisions.Select(r => MapRevision(r)).ToList();
                    wiItem.OriginId = issue.Key;
                    wiItem.Type = type;
                    wiItem.Revisions = revisions;
                }
                else
                {
                    Logger.Log(LogLevel.Error, $"Type mapping missing for '{issue.Key}' with Jira type '{issue.Type}'. Item was not exported which may cause missing links in issues referencing this item.");
                    return null;
                }
            }
            return wiItem;
        }

        private List<WiLink> MapLinks(JiraRevision r)
        {
            var links = new List<WiLink>();
            if (r.LinkActions == null)
                return links;

            // map issue links
            foreach (var jiraLinkAction in r.LinkActions)
            {
                var changeType = jiraLinkAction.ChangeType == RevisionChangeType.Added ? ReferenceChangeType.Added : ReferenceChangeType.Removed;

                var link = new WiLink();

                if (_config.LinkMap.Links != null)
                {
                    var linkType = (from t in _config.LinkMap.Links where t.Source == jiraLinkAction.Value.LinkType select t.Target).FirstOrDefault();

                    if (linkType != null)
                    {
                        link.Change = changeType;
                        link.SourceOriginId = jiraLinkAction.Value.SourceItem;
                        link.TargetOriginId = jiraLinkAction.Value.TargetItem;
                        link.WiType = linkType;

                        links.Add(link);
                    }
                }
            }

            // map epic link
            AddRemoveSingleLink(r, links, _jiraProvider.Settings.EpicLinkField, "Epic");

            // map parent
            AddRemoveSingleLink(r, links, "parent", "Parent");

            return links;
        }

        private List<WiAttachment> MapAttachments(JiraRevision rev)
        {
            var attachments = new List<WiAttachment>();
            if (rev.AttachmentActions == null)
                return attachments;

            _jiraProvider.DownloadAttachments(rev).Wait();

            foreach (var att in rev.AttachmentActions)
            {
                var change = att.ChangeType == RevisionChangeType.Added ? ReferenceChangeType.Added : ReferenceChangeType.Removed;

                var wiAtt = new WiAttachment()
                {
                    Change = change,
                    AttOriginId = att.Value.Id,
                    FilePath = att.Value.LocalPath,
                    Comment = "Imported from Jira"
                };
                attachments.Add(wiAtt);
            }

            return attachments;
        }

        private List<WiField> MapFields(JiraRevision r)
        {
            var fields = new List<WiField>();

            if (_config.TypeMap.Types != null)
            {
                var type = (from t in _config.TypeMap.Types where t.Source == r.Type select t.Target).FirstOrDefault();

                if (type != null && _fieldMappingsPerType.TryGetValue(type, out var mapping))
                {
                    foreach (var field in mapping)
                    {
                        try
                        {
                            var fieldreference = field.Key;
                            var (include, value) = field.Value(r);

                            if (include)
                            {
                                Logger.Log(LogLevel.Debug, $"Mapped value '{value}' to field '{fieldreference}'.");
                                fields.Add(new WiField()
                                {
                                    ReferenceName = fieldreference,
                                    Value = value
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(ex, $"Error mapping field '{field.Key}' on item '{r.OriginId}'.");
                        }
                    }
                }
            }

            return fields;
        }

        private HashSet<string> InitializeTypeMappings()
        {
            HashSet<string> types = new HashSet<string>();
            _config.TypeMap.Types.ForEach(t => types.Add(t.Target));
            return types;
        }

        private Dictionary<string, FieldMapping<JiraRevision>> InitializeFieldMappings()
        {
            Logger.Log(LogLevel.Info, "Initializing Jira field mapping...");

            var commonFields = new FieldMapping<JiraRevision>();
            var typeFields = new Dictionary<string, FieldMapping<JiraRevision>>();

            foreach (var targetType in _targetTypes)
            {
                if (!typeFields.ContainsKey(targetType))
                    typeFields.Add(targetType, new FieldMapping<JiraRevision>());
            }

            foreach (var item in _config.FieldMap.Fields)
            {
                if (item.Source != null)
                {
                    var isCustomField = item.SourceType == "name";
                    Func<JiraRevision, (bool, object)> value;

                    if (item.Mapping?.Values != null)
                    {
                        value = r => MapValue(r, item.Source);
                    }
                    else if (!string.IsNullOrWhiteSpace(item.Mapper))
                    {
                        switch (item.Mapper)
                        {
                            case "MapTitle":
                                value = r => MapTitle(r);
                                break;
                            case "MapTitleWithoutKey":
                                value = r => MapTitleWithoutKey(r);
                                break;
                            case "MapUser":
                                value = IfChanged<string>(item.Source, isCustomField, MapUser);
                                break;
                            case "MapSprint":
                                value = IfChanged<string>(item.Source, isCustomField, MapSprint);
                                break;
                            case "MapTags":
                                value = IfChanged<string>(item.Source, isCustomField, MapTags);
                                break;
                            case "MapArray":
                                value = IfChanged<string>(item.Source, isCustomField, MapArray);
                                break;
                            case "MapRemainingWork":
                                value = IfChanged<string>(item.Source, isCustomField, MapRemainingWork);
                                break;
                            case "MapRendered":
                                value = r => MapRenderedValue(r, item.Source, isCustomField);
                                break;
                            default:
                                value = IfChanged<string>(item.Source, isCustomField);
                                break;
                        }
                    }
                    else
                    {
                        var dataType = item.Type.ToLower();
                        if (dataType == "double")
                        {
                            value = IfChanged<double>(item.Source, isCustomField);
                        }
                        else if (dataType == "int" || dataType == "integer")
                        {
                            value = IfChanged<int>(item.Source, isCustomField);
                        }
                        else if (dataType == "datetime" || dataType == "date")
                        {
                            value = IfChanged<DateTime>(item.Source, isCustomField);
                        }
                        else
                        {
                            value = IfChanged<string>(item.Source, isCustomField);
                        }
                    }

                    // Check if not-for has been set, if so get all work item types except that one, else for has been set and get those                 
                    var currentWorkItemTypes = !string.IsNullOrWhiteSpace(item.NotFor) ? GetWorkItemTypes(item.NotFor.Split(',')) : item.For.Split(',').ToList();

                    foreach (var wit in currentWorkItemTypes)
                    {
                        try
                        {
                            if (wit == "All" || wit == "Common")
                            {
                                commonFields.Add(item.Target, value);
                            }
                            else
                            {
                                // If we haven't mapped the Type then we probably want to ignore the field
                                if (typeFields.TryGetValue(wit, out FieldMapping<JiraRevision> fm))
                                {
                                    fm.Add(item.Target, value);
                                }
                                else
                                {
                                    Logger.Log(LogLevel.Warning, $"No target type '{wit}' is set, field {item.Source} cannot be mapped.");
                                }
                            }
                        }

                        catch (Exception)
                        {
                            Logger.Log(LogLevel.Warning, $"Ignoring target mapping with key: '{item.Target}', because it is already configured.");
                            continue;
                        }
                    }
                }
            }

            // Now go through the list of built up type fields (which we will eventually get to
            // and then add them to the complete dictionary per type.
            var mappingPerWiType = new Dictionary<string, FieldMapping<JiraRevision>>();
            foreach (KeyValuePair<string, FieldMapping<JiraRevision>> item in typeFields)
            {
                mappingPerWiType.Add(item.Key, MergeMapping(commonFields, item.Value));
            }

            return mappingPerWiType;
        }

        private object MapRemainingWork(string seconds)
        {
            var secs = Convert.ToDouble(seconds);
            return TimeSpan.FromSeconds(secs).TotalHours;
        }

        private Func<JiraRevision, (bool, object)> IfChanged<T>(string sourceField, bool isCustomField, Func<T, object> mapperFunc = null)
        {
            if (isCustomField)
            {
                var customFieldName = _jiraProvider.GetCustomId(sourceField);
                sourceField = customFieldName;
            }

            return (r) =>
            {
                if (r.Fields.TryGetValue(sourceField.ToLower(), out object value))
                {
                    if (mapperFunc != null)
                    {
                        return (true, mapperFunc((T)value));
                    }
                    else
                    {
                        return (true, (T)value);
                    }
                }
                else
                {
                    return (false, null);
                }
            };
        }

        private (bool, object) MapTitle(JiraRevision r)
        {
            if (r.Fields.TryGetValue("summary", out object summary))
                return (true, $"[{r.ParentItem.Key}] {summary}");
            else
                return (false, null);
        }
        private (bool, object) MapTitleWithoutKey(JiraRevision r)
        {
            if (r.Fields.TryGetValue("summary", out object summary))
                return (true, summary);
            else
                return (false, null);
        }

        private (bool, object) MapValue(JiraRevision r, string itemSource)
        {
            var targetWit = (from t in _config.TypeMap.Types where t.Source == r.Type select t.Target).FirstOrDefault();

            if (r.Fields.TryGetValue(itemSource, out object value))
            {
                foreach (var item in _config.FieldMap.Fields)
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

        private (bool, object) MapRenderedValue(JiraRevision r, string sourceField, bool isCustomField)
        {
            if (isCustomField)
            {
                var customFieldName = _jiraProvider.GetCustomId(sourceField);
                sourceField = customFieldName;
            }
            var fieldName = sourceField + "$Rendered";

            var targetWit = (from t in _config.TypeMap.Types where t.Source == r.Type select t.Target).FirstOrDefault();

            if (r.Fields.TryGetValue(fieldName, out object value))
            {
                foreach (var item in _config.FieldMap.Fields)
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

        private object MapTags(string labels)
        {
            if (string.IsNullOrWhiteSpace(labels))
                return null;

            var tags = labels.Split(' ');
            if (!tags.Any())
                return null;
            else
                return string.Join(";", tags);
        }

        private object MapArray(string field)
        {
            if (string.IsNullOrWhiteSpace(field))
                return null;

            var values = field.Split(',');
            if (!values.Any())
                return null;
            else
                return string.Join(";", values);
        }

        private object MapSprint(string iterationPathsString)
        {
            if (string.IsNullOrWhiteSpace(iterationPathsString))
                return null;

            var iterationPaths = iterationPathsString.Split(',').AsEnumerable();
            iterationPaths = iterationPaths.Select(ip => ip.Trim());

            var iterationPath = iterationPaths.Last();

            return iterationPath;
        }

        private string CorrectRenderedHtmlvalue(object value, JiraRevision revision)
        {
            var htmlValue = value.ToString();

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

        #endregion
    }
}