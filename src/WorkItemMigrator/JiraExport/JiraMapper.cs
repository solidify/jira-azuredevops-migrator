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
        private readonly JiraProvider jiraProvider;
        private readonly Dictionary<string, FieldMapping<JiraRevision>> _fieldMappingsPerType;

        public JiraMapper(JiraProvider jiraProvider, ConfigJson config) : base(jiraProvider?.Settings?.UserMappingFile)
        {
            this.jiraProvider = jiraProvider;

            _fieldMappingsPerType = InitializeFieldMappings(config);
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
                var linkType = MapLinkType(type);

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

        private List<string> GetWorkItemTypes(string notFor = "")
        {
            return !string.IsNullOrWhiteSpace(notFor) ? WorkItemType.GetWorkItemTypes(notFor) : WorkItemType.GetWorkItemTypes();
        }

        #region Mapping definitions

        private WiRevision MapRevision(JiraRevision r, TemplateType template)
        {
            List<WiAttachment> attachments = MapAttachments(r);
            List<Migration.WIContract.WiField> fields = MapFields(r, template);
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
            if (string.IsNullOrWhiteSpace(sourceUser))
                return null;

            var email = jiraProvider.GetUserEmail(sourceUser);
            return base.MapUser(email);
        }

        internal WiItem Map(JiraItem issue, TemplateType template)
        {
            string type = MapType(issue.Type, template);
            var revisions = issue.Revisions.Select(r => MapRevision(r, template)).ToList();
            MapLastDescription(revisions, issue);

            return new WiItem()
            {
                OriginId = issue.Key,
                Type = type,
                Revisions = revisions
            };
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
                var linkType = MapLinkType(jiraLinkAction.Value.LinkType);

                if (linkType != null)
                {
                    var link = new WiLink()
                    {
                        Change = changeType,
                        SourceOriginId = jiraLinkAction.Value.SourceItem,
                        TargetOriginId = jiraLinkAction.Value.TargetItem,
                        WiType = linkType
                    };

                    links.Add(link);
                }
            }

            // map epic link
            AddRemoveSingleLink(r, links, jiraProvider.Settings.EpicLinkField, "Epic");

            // map parent
            AddRemoveSingleLink(r, links, "parent", "Parent");

            return links;
        }

        private List<WiAttachment> MapAttachments(JiraRevision rev)
        {
            var attachments = new List<WiAttachment>();
            if (rev.AttachmentActions == null)
                return attachments;

            jiraProvider.DownloadAttachments(rev).Wait();

            foreach (var att in rev.AttachmentActions)
            {
                var change = att.ChangeType == RevisionChangeType.Added ? ReferenceChangeType.Added : ReferenceChangeType.Removed;

                var wiAtt = new WiAttachment()
                {
                    Change = change,
                    AttOriginId = att.Value.Id,
                    FilePath = att.Value.LocalPath,
                    Comment = "Imported from Jira" // customization point
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

        private List<Migration.WIContract.WiField> MapFields(JiraRevision r, TemplateType template)
        {
            List<Migration.WIContract.WiField> fields = new List<Migration.WIContract.WiField>();
            string type = MapType(r.ParentItem.Type, template);

            if (_fieldMappingsPerType.TryGetValue(type, out var mapping))
            {
                foreach (var field in mapping)
                {
                    try
                    {
                        var fieldreference = field.Key;
                        var (include, value) = field.Value(r);

                        if (include)
                        {
                            fields.Add(new Migration.WIContract.WiField()
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

            return fields;
        }

        private string MapLinkType(string linkType)
        {
            switch (linkType)
            {
                case "Epic": return "System.LinkTypes.Hierarchy-Reverse";
                case "Parent": return "System.LinkTypes.Hierarchy-Reverse";
                case "Relates": return "System.LinkTypes.Related";
                case "Duplicate": return "System.LinkTypes.Duplicate-Forward";
                default: return "System.LinkTypes.Related";
            }
        }

        private Dictionary<string, FieldMapping<JiraRevision>> InitializeFieldMappings(ConfigJson config)
        {
            var mappingPerWiType = new Dictionary<string, FieldMapping<JiraRevision>>();
            var commonFields = new FieldMapping<JiraRevision>();
            var bugFields = new FieldMapping<JiraRevision>();
            var taskFields = new FieldMapping<JiraRevision>();
            var pbiFields = new FieldMapping<JiraRevision>();
            var epicFields = new FieldMapping<JiraRevision>();
            var featureFields = new FieldMapping<JiraRevision>();
            var requirementFields = new FieldMapping<JiraRevision>();
            var userStoryFields = new FieldMapping<JiraRevision>();
            List<string> witList = null;
            var processFields = (from f in config.FieldMap.Fields where f.Process == "Common" || f.Process == "All" || f.Process == config.ProcessTemplate select f).ToList();
            foreach (var item in processFields)
            {
                if (item.Source != null)
                {
                    // If not-for and for has not been set (should never happen) then get all work item types
                    if (string.IsNullOrWhiteSpace(item.NotFor) && string.IsNullOrWhiteSpace(item.For))
                    {
                        if (witList == null)
                        {
                            witList = WorkItemType.GetWorkItemTypes();
                        }
                    }
                    else
                    {
                        // Check if not-for has been set, if so get all work item types except that one, else for has been set and get those
                        witList = !string.IsNullOrWhiteSpace(item.NotFor) ? GetWorkItemTypes(item.NotFor) : item.For.Split(',').ToList();
                    }

                    Func<JiraRevision, (bool, object)> value;
                    if (item.Type == "string")
                    {
                        switch (item.Mapper)
                        {
                            case "MapTitle":
                                value = r => MapTitle(r);
                                break;
                            case "MapUser":
                                value = IfChanged<string>(item.Source, MapUser);
                                break;
                            case "MapPriority":
                                value = IfChanged<string>(item.Source, MapPriority);
                                break;
                            case "MapSprint":
                                value = IfChanged<string>(jiraProvider.Settings.SprintField, MapSprint);
                                break;
                            case "MapTags":
                                value = IfChanged<string>(item.Source, MapTags);
                                break;
                            case "MapStateTask":
                                value = IfChanged<string>(item.Source, MapStateTask);
                                break;
                            case "MapStateBugAndPBI":
                                value = IfChanged<string>(item.Source, MapStateBugAndPBI);
                                break;
                            default:
                                value = IfChanged<string>(item.Source);
                                break;
                        }
                    }
                    else if (item.Type == "int")
                    {
                        value = IfChanged<int>(item.Source);
                    }
                    else if (item.Type == "double")
                    {
                        value = IfChanged<double>(item.Source);
                    }
                    else
                    {
                        // Mainly a fallback if no data type is set or is misspelled
                        value = IfChanged<string>(item.Source);
                    }

                    foreach (var wit in witList)
                    {
                        if (wit == "All" || wit == "Common")
                        {
                            commonFields.Add(item.Target, value);
                        }
                        if (wit == WorkItemType.Bug)
                        {
                            bugFields.Add(item.Target, value);
                        }
                        if (wit == WorkItemType.Epic)
                        {
                            epicFields.Add(item.Target, value);
                        }
                        if (wit == WorkItemType.Feature)
                        {
                            featureFields.Add(item.Target, value);
                        }
                        if (wit == WorkItemType.ProductBacklogItem)
                        {
                            pbiFields.Add(item.Target, value);
                        }
                        if (wit == WorkItemType.Requirement)
                        {
                            requirementFields.Add(item.Target, value);
                        }
                        if (wit == WorkItemType.Task)
                        {
                            taskFields.Add(item.Target, value);
                        }
                        if (wit == WorkItemType.UserStory)
                        {
                            userStoryFields.Add(item.Target, value);
                        }
                    }
                }
            }

            mappingPerWiType.Add(WorkItemType.Bug, MergeMapping(commonFields, bugFields, taskFields));
            mappingPerWiType.Add(WorkItemType.ProductBacklogItem, MergeMapping(commonFields, pbiFields));
            mappingPerWiType.Add(WorkItemType.Task, MergeMapping(commonFields, bugFields, taskFields));
            mappingPerWiType.Add(WorkItemType.Feature, MergeMapping(commonFields, featureFields));
            mappingPerWiType.Add(WorkItemType.Epic, MergeMapping(commonFields, epicFields));
            mappingPerWiType.Add(WorkItemType.Requirement, MergeMapping(commonFields, requirementFields));
            mappingPerWiType.Add(WorkItemType.UserStory, MergeMapping(commonFields, userStoryFields));

            return mappingPerWiType;
        }

        private static Func<JiraRevision, (bool, object)> IfChanged<T>(string sourceField, Func<T, object> mapperFunc)
        {
            return (r) =>
            {
                if (r.Fields.TryGetValue(sourceField, out object value))
                    return (true, mapperFunc((T)value));
                else
                    return (false, null);
            };
        }

        private static Func<JiraRevision, (bool, object)> IfChanged<T>(string sourceField)
        {
            return (r) =>
            {
                if (r.Fields.TryGetValue(sourceField, out object value))
                    return (true, (T)value);
                else
                    return (false, null);
            };
        }

        private (bool, object) MapTitle(JiraRevision r)
        {
            if (r.Fields.TryGetValue("summary", out object summary))
                return (true, $"[{r.ParentItem.Key}] {summary}");
            else
                return (false, null);
        }

        private string MapStateTask(string jiraState)
        {
            jiraState = jiraState.ToLowerInvariant();
            switch (jiraState)
            {
                case "to do": return "To Do";
                case "done": return "Done";
                case "in progress": return "In Progress";
                case "ready for test": return "Ready for test";
                default: return "To Do";
            }
        }

        private object MapStateBugAndPBI(string jiraState)
        {
            jiraState = jiraState.ToLowerInvariant();
            switch (jiraState)
            {
                case "to do": return "New";
                case "done": return "Done";
                case "in progress": return "Committed";
                default: return "Committed";
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

        private object MapPriority(string jiraPriority)
        {
            switch (jiraPriority.ToLowerInvariant())
            {
                case "blocker":
                case "critical":
                case "highest": return 1;
                case "major":
                case "high": return 2;
                case "medium":
                case "low": return 3;
                case "lowest":
                case "minor":
                case "trivial": return 4;
                default: return null;
            }
        }

        //private object MapSeverity(string jiraSeverity)
        //{
        //    switch (jiraSeverity.ToLowerInvariant())
        //    {
        //        case "blocker":
        //        case "critical":
        //        case "highest": return 1;
        //        case "major":
        //        case "high": return 2;
        //        case "medium":
        //        case "low": return 3;
        //        case "lowest":
        //        case "minor":
        //        case "trivial": return 4;
        //        default: return null;
        //    }
        //}

        protected string MapType(string type, TemplateType template)
        {
            string backlogItem;
            switch (template)
            {
                case TemplateType.Scrum:
                    backlogItem = WorkItemType.ProductBacklogItem;
                    break;
                case TemplateType.Agile:
                    backlogItem = WorkItemType.UserStory;
                    break;
                case TemplateType.CMMI:
                    backlogItem = WorkItemType.Requirement;
                    break;
                default:
                    backlogItem = WorkItemType.ProductBacklogItem;
                    break;
            }

            switch (type)
            {
                case "Task": return backlogItem;
                case "Sub-task": return WorkItemType.Task;
                case "Story": return backlogItem;
                case "Bug": return WorkItemType.Bug;
                case "Epic": return WorkItemType.Feature;
                default: return backlogItem;
            }
        }

        private void MapLastDescription(List<WiRevision> revisions, JiraItem issue)
        {
            var descFieldName = issue.Type == "Bug" ? "Microsoft.VSTS.TCM.ReproSteps" : "System.Description";
            var lastDescUpdateRev =
                ((IEnumerable<WiRevision>)revisions)
               .Reverse()
               .FirstOrDefault(r => r.Fields.Any(i => i.ReferenceName.Equals(descFieldName, StringComparison.InvariantCultureIgnoreCase)));
            var lastDescUpdate = lastDescUpdateRev
                                ?.Fields
                                ?.FirstOrDefault(i => i.ReferenceName.Equals(descFieldName, StringComparison.InvariantCultureIgnoreCase));
            var renderedDescription = MapRenderedDescription(issue);

            if (lastDescUpdate == null && !string.IsNullOrWhiteSpace(renderedDescription))
            {
                lastDescUpdate = new Migration.WIContract.WiField() { ReferenceName = descFieldName, Value = renderedDescription };
                lastDescUpdateRev = revisions.First();
                lastDescUpdateRev.Fields.Add(lastDescUpdate);
            }

            if (lastDescUpdate != null)
            {
                lastDescUpdate.Value = renderedDescription;
            }

            lastDescUpdateRev.AttachmentReferences = true;
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