using Atlassian.Jira;
using Migration.Common;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Migration.Common.Log;

namespace JiraExport
{
    public class JiraItem
    {
        #region Static

        public static JiraItem CreateFromRest(string issueKey, JiraProvider jiraProvider)
        {
            var remoteIssue = jiraProvider.DownloadIssue(issueKey);
            Logger.Log(LogLevel.Debug, $"Downloaded item.");

            var jiraItem = new JiraItem(jiraProvider, remoteIssue);
            var revisions = BuildRevisions(jiraItem, jiraProvider);
            jiraItem.Revisions = revisions;
            Logger.Log(LogLevel.Debug, $"Created {revisions.Count} history revisions.");

            return jiraItem;
        }

        private static List<JiraRevision> BuildRevisions(JiraItem jiraItem, JiraProvider jiraProvider)
        {
            string issueKey = jiraItem.Key;
            var remoteIssue = jiraItem.RemoteIssue;
            Dictionary<string, object> fields = ExtractFields(issueKey, (JObject)remoteIssue.SelectToken("$.fields"), jiraProvider);
            List<JiraAttachment> attachments = ExtractAttachments(remoteIssue.SelectTokens("$.fields.attachment[*]").Cast<JObject>()) ?? new List<JiraAttachment>();
            List<JiraLink> links = ExtractLinks(issueKey, remoteIssue.SelectTokens("$.fields.issuelinks[*]").Cast<JObject>()) ?? new List<JiraLink>();

            var changelog = jiraProvider.DownloadChangelog(issueKey).ToList();
            changelog.Reverse();

            Stack<JiraRevision> revisions = new Stack<JiraRevision>();

            foreach (var change in changelog)
            {
                DateTime created = change.ExValue<DateTime>("$.created");
                string author = change.ExValue<string>("$.author.name");

                List<RevisionAction<JiraLink>> linkChanges = new List<RevisionAction<JiraLink>>();
                List<RevisionAction<JiraAttachment>> attachmentChanges = new List<RevisionAction<JiraAttachment>>();
                Dictionary<string, object> fieldChanges = new Dictionary<string, object>();

                var items = change.SelectTokens("$.items[*]").Cast<JObject>().Select(i => new JiraChangeItem(i));
                foreach (var item in items)
                {
                    if (item.Field == "Link")
                    {
                        var linkChange = TransformLinkChange(item, issueKey, jiraProvider);
                        if (linkChange == null)
                            continue;

                        linkChanges.Add(linkChange);

                        UndoLinkChange(linkChange, links);
                    }
                    else if (item.Field == "Attachment")
                    {
                        var attachmentChange = TransformAttachmentChange(item);
                        if (attachmentChange == null)
                            continue;

                        attachmentChanges.Add(attachmentChange);

                        UndoAttachmentChange(attachmentChange, attachments);
                    }
                    else
                    {
                        var (fieldref, from, to) = TransformFieldChange(item, jiraProvider);

                        fieldChanges[fieldref] = to;

                        // undo field change
                        if (string.IsNullOrEmpty(from))
                            fields.Remove(fieldref);
                        else
                            fields[fieldref] = from;
                    }
                }

                var revision = new JiraRevision(jiraItem) { Time = created, Author = author, AttachmentActions = attachmentChanges, LinkActions = linkChanges, Fields = fieldChanges };
                revisions.Push(revision);
            }

            // what is left after undoing all changes is first revision
            var attActions = attachments.Select(a => new RevisionAction<JiraAttachment>() { ChangeType = RevisionChangeType.Added, Value = a }).ToList();
            var linkActions = links.Select(l => new RevisionAction<JiraLink>() { ChangeType = RevisionChangeType.Added, Value = l }).ToList();
            var fieldActions = fields;

            var reporter = (string)fields["reporter"];
            var createdOn = (DateTime)fields["created"];

            var firstRevision = new JiraRevision(jiraItem) { Time = createdOn, Author = reporter, AttachmentActions = attActions, Fields = fieldActions, LinkActions = linkActions };
            revisions.Push(firstRevision);
            var listOfRevisions = revisions.ToList();

            List<JiraRevision> commentRevisions = BuildCommentRevisions(jiraItem, jiraProvider);
            listOfRevisions.AddRange(commentRevisions);
            listOfRevisions.Sort();

            foreach (var revAndI in listOfRevisions.Select((r, i) => (r, i)))
                revAndI.Item1.Index = revAndI.Item2;

            return listOfRevisions;
        }

        private static List<JiraRevision> BuildCommentRevisions(JiraItem jiraItem, JiraProvider jiraProvider)
        {
            var comments = jiraProvider.Jira.Issues.GetCommentsAsync(jiraItem.Key).Result;
            return comments.Select(c => new JiraRevision(jiraItem)
            {
                Author = c.Author,
                Time = c.CreatedDate.Value,
                Fields = new Dictionary<string, object>() { { "comment", c.Body } },
                AttachmentActions = new List<RevisionAction<JiraAttachment>>(),
                LinkActions = new List<RevisionAction<JiraLink>>()
            }).ToList();
        }

        private static void UndoAttachmentChange(RevisionAction<JiraAttachment> attachmentChange, List<JiraAttachment> attachments)
        {
            if (attachmentChange.ChangeType == RevisionChangeType.Removed)
            {
                Logger.Log(LogLevel.Debug, $"Skipping undo for attachment '{attachmentChange.ToString()}'.");
                return;
            }

            if (attachments.Remove(attachmentChange.Value))
                Logger.Log(LogLevel.Debug, $"Undone attachment '{attachmentChange.ToString()}'.");
            else
                Logger.Log(LogLevel.Debug, $"No attachment to undo for '{attachmentChange.ToString()}'.");
        }

        private static RevisionAction<JiraAttachment> TransformAttachmentChange(JiraChangeItem item)
        {
            string attKey = string.Empty;
            string attFilename = string.Empty;

            RevisionChangeType changeType;

            if (item.From == null && item.To != null)
            {
                attKey = item.To;
                attFilename = item.ToString;
                changeType = RevisionChangeType.Added;
            }
            else if (item.To == null && item.From != null)
            {
                attKey = item.From;
                attFilename = item.FromString;
                changeType = RevisionChangeType.Removed;
            }
            else
            {
                Logger.Log(LogLevel.Error, "Attachment change not handled!");
                return null;
            }

            return new RevisionAction<JiraAttachment>()
            {
                ChangeType = changeType,
                Value = new JiraAttachment()
                {
                    Id = attKey,
                    Filename = attFilename
                }
            };
        }

        private static (string, string, string) TransformFieldChange(JiraChangeItem item, JiraProvider jira)
        {
            var objectFields = new HashSet<string>() { "assignee", "creator", "reporter" };
            string from, to = string.Empty;

            string fieldId = item.FieldId ?? GetCustomFieldId(item.Field, jira) ?? item.Field;

            if (objectFields.Contains(fieldId))
            {
                from = item.From;
                to = item.To;
            }
            else
            {
                from = item.FromString;
                to = item.ToString;
            }

            return (fieldId, from, to);
        }

        private static string GetCustomFieldId(string fieldName, JiraProvider jira)
        {
            if (jira.Jira.RestClient.Settings.Cache.CustomFields.TryGetValue(fieldName, out var customField))
                return customField.Id;
            else return null;

        }

        private static void UndoLinkChange(RevisionAction<JiraLink> linkChange, List<JiraLink> links)
        {
            if (linkChange.ChangeType == RevisionChangeType.Removed)
            {
                Logger.Log(LogLevel.Debug, $"Skipping undo for link '{linkChange.ToString()}'.");
                return;
            }

            if (links.Remove(linkChange.Value))
                Logger.Log(LogLevel.Debug, $"Undone link '{linkChange.ToString()}'.");
            else
                Logger.Log(LogLevel.Debug, $"No link to undo for '{linkChange.ToString()}'");
        }

        private static RevisionAction<JiraLink> TransformLinkChange(JiraChangeItem item, string sourceItemKey, JiraProvider jira)
        {
            string targetItemKey = string.Empty;
            string linkTypeString = string.Empty;
            RevisionChangeType changeType;

            if (item.From == null && item.To != null)
            {
                targetItemKey = item.To;
                linkTypeString = item.ToString;
                changeType = RevisionChangeType.Added;
            }
            else if (item.To == null && item.From != null)
            {
                targetItemKey = item.From;
                linkTypeString = item.FromString;
                changeType = RevisionChangeType.Removed;
            }
            else
            {
                Logger.Log(LogLevel.Error, $"Link change not handled!");
                return null;
            }

            var linkType = jira.LinkTypes.FirstOrDefault(lt => linkTypeString.EndsWith(lt.Outward + " " + targetItemKey));
            if (linkType == null)
            {
                Logger.Log(LogLevel.Debug, $"Link with description '{linkTypeString}' is either not found or this issue ({sourceItemKey}) is not inward issue.");
                return null;
            }
            else
            {
                if (linkType.Inward == linkType.Outward && sourceItemKey.CompareTo(targetItemKey) < 0)
                {
                    Logger.Log(LogLevel.Debug, $"Link is non-directional ({linkType.Name}) and sourceItem ({sourceItemKey}) is older then target item ({targetItemKey}). Link change will be part of target item.");
                    return null;
                }

                return new RevisionAction<JiraLink>()
                {
                    ChangeType = changeType,
                    Value = new JiraLink()
                    {
                        SourceItem = sourceItemKey,
                        TargetItem = targetItemKey,
                        LinkType = linkType.Name,
                    }
                };
            }
        }

        private static List<JiraLink> ExtractLinks(string sourceKey, IEnumerable<JObject> issueLinks)
        {
            var links = new List<JiraLink>();

            foreach (var issueLink in issueLinks)
            {
                var targetIssueKey = issueLink.ExValue<string>("$.outwardIssue.key");
                if (string.IsNullOrWhiteSpace(targetIssueKey))
                    continue;

                var type = issueLink.ExValue<string>("$.type.name");

                var link = new JiraLink() { SourceItem = sourceKey, TargetItem = targetIssueKey, LinkType = type };
                links.Add(link);
            }

            return links;
        }

        private static List<JiraAttachment> ExtractAttachments(IEnumerable<JObject> attachmentObjs)
        {
            return attachmentObjs.Select(attObj =>
            {
                return new JiraAttachment
                {
                    Id = attObj.ExValue<string>("$.id"),
                    Filename = attObj.ExValue<string>("$.filename"),
                    Url = attObj.ExValue<string>("$.content"),
                    ThumbUrl = attObj.ExValue<string>("$.thumbnail")
                };
            }).ToList();
        }

        private static Dictionary<string, Func<JToken, object>> _fieldExtractionMapping = null;
        private static Dictionary<string, object> ExtractFields(string key, JObject remoteFields, JiraProvider jira)
        {
            var fields = new Dictionary<string, object>();

            var extractName = new Func<JToken, object>((t) => t.ExValue<string>("$.name"));

            if (_fieldExtractionMapping == null)
            {
                _fieldExtractionMapping = new Dictionary<string, Func<JToken, object>>()
                    {
                        { "priority", extractName },
                        { "labels", t => t.Values<string>().Any() ? string.Join(" ", t.Values<string>()) : null },
                        { "assignee", extractName },
                        { "creator", extractName },
                        { "reporter", extractName},
                        { jira.Settings.SprintField, t => string.Join(", ", ParseCustomField(jira.Settings.SprintField, t, jira)) },
                        { "status", extractName },
                        { "parent", t => t.ExValue<string>("$.key") }
                    };
            }

            foreach (var prop in remoteFields.Properties())
            {
                var type = prop.Value.Type;
                var name = prop.Name.ToLower();
                object value = null;

                if (_fieldExtractionMapping.TryGetValue(name, out Func<JToken, object> mapping))
                {
                    value = mapping(prop.Value);
                }
                else if (type == JTokenType.String || type == JTokenType.Integer || type == JTokenType.Float)
                {
                    value = prop.Value.Value<string>();
                }
                else if (prop.Value.Type == JTokenType.Date)
                {
                    value = prop.Value.Value<DateTime>();
                }
                else if (type == JTokenType.Array && prop.Value.Any())
                {
                    value = string.Join(";", prop.Value.Select(st => st.ExValue<string>("$.name")).ToList());
                    if ((string)value == ";")
                        value = string.Join(";", prop.Value.Select(st => st.ExValue<string>("$.value")).ToList());
                }

                if (value != null)
                {
                    fields[name] = value;
                }
            }

            fields["key"] = key;
            fields["issuekey"] = key;

            return fields;
        }

        private static string[] ParseCustomField(string fieldName, JToken value, JiraProvider provider)
        {
            var serializedValue = new string[] { };

            if (provider.Jira.RestClient.Settings.Cache.CustomFields.TryGetValue(fieldName, out var customField) &&
                customField != null &&
                provider.Jira.RestClient.Settings.CustomFieldSerializers.TryGetValue(customField.CustomType, out var serializer))
            {
                serializedValue = serializer.FromJson(value);
            }

            return serializedValue;
        }

        #endregion

        private readonly JiraProvider _provider;

        public string Key { get { return RemoteIssue.ExValue<string>("$.key"); } }
        public string Type { get { return RemoteIssue.ExValue<string>("$.fields.issuetype.name")?.Trim(); } }

        public string EpicParent { get { return RemoteIssue.ExValue<string>($"$.fields.{_provider.Settings.EpicLinkField}"); } }
        public string Parent { get { return RemoteIssue.ExValue<string>("$.fields.parent.key"); } }
        public List<string> SubItems { get { return RemoteIssue.SelectTokens("$.fields.subtasks.[*]", false).Select(st => st.ExValue<string>("$.key")).ToList(); } }

        public JObject RemoteIssue { get; private set; }

        public List<JiraRevision> Revisions { get; set; }

        private JiraItem(JiraProvider provider, JObject remoteIssue)
        {
            this._provider = provider;
            RemoteIssue = remoteIssue;
        }

        internal string GetUserEmail(string author)
        {
            return _provider.GetUserEmail(author);
        }
    }
}
