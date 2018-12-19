using System;
using System.Collections.Generic;
using System.Linq;
using Migration.Common;
using System.Text.RegularExpressions;
using Common.Config;
using Migration.Common.Config;
using System.Reflection;
using Newtonsoft.Json;
using Migration.WIContract;

namespace JiraExport
{
    internal class JiraMapper : BaseMapper<JiraRevision>
    {
        private readonly JiraProvider _jiraProvider;
        private readonly Dictionary<string, FieldMapping<JiraRevision>> _fieldMappingsPerType;
        private readonly ConfigJson _config;

        public JiraMapper(JiraProvider jiraProvider, ConfigJson config) : base(jiraProvider?.Settings?.UserMappingFile)
        {
            _jiraProvider = jiraProvider;
            _config = config;
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
                list = WorkItemType.GetWorkItemTypes(notFor);
            }
            else
            {
                list = WorkItemType.GetWorkItemTypes();
            }
            return list;
        }

        #region Mapping definitions

        private WiRevision MapRevision(JiraRevision r)
        {
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
                Links = links
            };
        }

        protected override string MapUser(string sourceUser)
        {
            var user = string.Empty;
            if (!string.IsNullOrWhiteSpace(sourceUser))
            {
                var email = _jiraProvider.GetUserEmail(sourceUser);
                user = base.MapUser(email);
            }
            else
            {
                user = base.MapUser(sourceUser);
            }
            return user;
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
                    MapLastDescription(revisions, issue);

                    wiItem.OriginId = issue.Key;
                    wiItem.Type = type;
                    wiItem.Revisions = revisions;
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

                if (!string.IsNullOrWhiteSpace(att.Value.LocalThumbPath))
                {
                    var wiThumbAtt = new WiAttachment()
                    {
                        Change = change,
                        AttOriginId = att.Value.Id + "-thumb",
                        FilePath = att.Value.LocalThumbPath,
                        Comment = $"Thumbnail for {att.Value.Filename}"
                    };

                    attachments.Add(wiThumbAtt);
                }
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
                                fields.Add(new WiField()
                                {
                                    ReferenceName = fieldreference,
                                    Value = value
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(LogLevel.Error, $"Error mapping field {field.Key} on item {r.OriginId}: {ex.Message}");
                        }
                    }
                }
            }

            return fields;
        }

        private Dictionary<string, FieldMapping<JiraRevision>> InitializeFieldMappings()
        {
            var commonFields = new FieldMapping<JiraRevision>();
            var bugFields = new FieldMapping<JiraRevision>();
            var taskFields = new FieldMapping<JiraRevision>();
            var pbiFields = new FieldMapping<JiraRevision>();
            var epicFields = new FieldMapping<JiraRevision>();
            var featureFields = new FieldMapping<JiraRevision>();
            var requirementFields = new FieldMapping<JiraRevision>();
            var userStoryFields = new FieldMapping<JiraRevision>();

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
                            case "MapUser":
                                value = IfChanged<string>(item.Source, isCustomField, MapUser);
                                break;
                            case "MapSprint":
                                value = IfChanged<string>(item.Source, isCustomField, MapSprint);
                                break;
                            case "MapTags":
                                value = IfChanged<string>(item.Source, isCustomField, MapTags);
                                break;
                            case "MapRemainingWork":
                                value = IfChanged<string>(item.Source, isCustomField, MapRemainingWork);
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
                        if (wit == "All" || wit == "Common")
                        {
                            commonFields.Add(item.Target, value);
                        }
                        else if (wit == WorkItemType.Bug)
                        {
                            bugFields.Add(item.Target, value);
                        }
                        else if (wit == WorkItemType.Epic)
                        {
                            epicFields.Add(item.Target, value);
                        }
                        else if (wit == WorkItemType.Feature)
                        {
                            featureFields.Add(item.Target, value);
                        }
                        else if (wit == WorkItemType.ProductBacklogItem)
                        {
                            pbiFields.Add(item.Target, value);
                        }
                        else if (wit == WorkItemType.Requirement)
                        {
                            requirementFields.Add(item.Target, value);
                        }
                        else if (wit == WorkItemType.Task)
                        {
                            taskFields.Add(item.Target, value);
                        }
                        else if (wit == WorkItemType.UserStory)
                        {
                            userStoryFields.Add(item.Target, value);
                        }
                    }
                }
            }

            var mappingPerWiType = new Dictionary<string, FieldMapping<JiraRevision>>
            {
                { WorkItemType.Bug, MergeMapping(commonFields, bugFields, taskFields) },
                { WorkItemType.ProductBacklogItem, MergeMapping(commonFields, pbiFields) },
                { WorkItemType.Task, MergeMapping(commonFields, bugFields, taskFields) },
                { WorkItemType.Feature, MergeMapping(commonFields, featureFields) },
                { WorkItemType.Epic, MergeMapping(commonFields, epicFields) },
                { WorkItemType.Requirement, MergeMapping(commonFields, requirementFields) },
                { WorkItemType.UserStory, MergeMapping(commonFields, userStoryFields) }
            };

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

        private object MapSprint(string iterationPathsString)
        {
            if (string.IsNullOrWhiteSpace(iterationPathsString))
                return null;

            var iterationPaths = iterationPathsString.Split(',').AsEnumerable();
            iterationPaths = iterationPaths.Select(ip => ip.Trim());

            var iterationPath = iterationPaths.Last();

            return iterationPath;
        }

        private void MapLastDescription(List<WiRevision> revisions, JiraItem issue)
        {
            var descFieldName = issue.Type == "Bug" ? "Microsoft.VSTS.TCM.ReproSteps" : "System.Description";

            var lastDescUpdateRev = ((IEnumerable<WiRevision>)revisions)
                                        .Reverse()
                                        .FirstOrDefault(r => r.Fields.Any(i => i.ReferenceName.Equals(descFieldName, StringComparison.InvariantCultureIgnoreCase)));

            if (lastDescUpdateRev != null)
            {
                var lastDescUpdate = lastDescUpdateRev?.Fields?.FirstOrDefault(i => i.ReferenceName.Equals(descFieldName, StringComparison.InvariantCultureIgnoreCase));
                var renderedDescription = MapRenderedDescription(issue);

                if (lastDescUpdate == null && !string.IsNullOrWhiteSpace(renderedDescription))
                {
                    lastDescUpdate = new WiField() { ReferenceName = descFieldName, Value = renderedDescription };
                    lastDescUpdateRev = revisions.First();
                    lastDescUpdateRev.Fields.Add(lastDescUpdate);
                }

                if (lastDescUpdate != null)
                {
                    lastDescUpdate.Value = renderedDescription;
                }

                lastDescUpdateRev.AttachmentReferences = true;
            }
        }

        private string MapRenderedDescription(JiraItem issue)
        {
            string originalHtml = issue.RemoteIssue.ExValue<string>("$.renderedFields.description");
            string wiHtml = originalHtml;

            foreach (var att in issue.Revisions.SelectMany(r => r.AttachmentActions.Where(aa => aa.ChangeType == RevisionChangeType.Added).Select(aa => aa.Value)))
            {
                if (!string.IsNullOrWhiteSpace(att.ThumbUrl) && wiHtml.Contains(att.ThumbUrl))
                    wiHtml = wiHtml.Replace(att.ThumbUrl, att.ThumbUrl);

                if (!string.IsNullOrWhiteSpace(att.Url) && wiHtml.Contains(att.Url))
                    wiHtml = wiHtml.Replace(att.Url, att.Url);
            }

            string imageWrapPattern = "<span class=\"image-wrap\".*?>.*?(<img .*? />).*?</span>";
            wiHtml = Regex.Replace(wiHtml, imageWrapPattern, m => m.Groups[1]?.Value);

            string userLinkPattern = "<a href=.*? class=\"user-hover\" .*?>(.*?)</a>";
            wiHtml = Regex.Replace(wiHtml, userLinkPattern, m => m.Groups[1]?.Value);

            string css = ReadEmbeddedFile("JiraExport.jirastyles.css");
            if (string.IsNullOrWhiteSpace(css))
                Logger.Log(LogLevel.Warning, "Could not read css styles for description.");
            else
                wiHtml = "<style>" + css + "</style>" + wiHtml;


            return wiHtml ?? string.Empty;
        }

        #endregion
    }
}