﻿using Atlassian.Jira;
using Migration.Common;
using Migration.Common.Log;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace JiraExport
{
    public class JiraItem
    {
        #region Static

        public static JiraItem CreateFromRest(string issueKey, IJiraProvider jiraProvider)
        {
            var remoteIssue = jiraProvider.DownloadIssue(issueKey);
            if (remoteIssue == null)
                return default(JiraItem);

            Logger.Log(LogLevel.Debug, $"Downloaded item.");

            var jiraItem = new JiraItem(jiraProvider, remoteIssue);
            var revisions = BuildRevisions(jiraItem, jiraProvider);
            jiraItem.Revisions = revisions;
            Logger.Log(LogLevel.Debug, $"Created {revisions.Count} history revisions.");

            return jiraItem;

        }

        private static List<JiraRevision> BuildRevisions(JiraItem jiraItem, IJiraProvider jiraProvider)
        {
            string issueKey = jiraItem.Key;
            var remoteIssue = jiraItem.RemoteIssue;
            Dictionary<string, object> fields = ExtractFields(issueKey, remoteIssue, jiraProvider);

            List<JiraAttachment> attachments = ExtractAttachments(remoteIssue.SelectTokens("$.fields.attachment[*]").Cast<JObject>()) ?? new List<JiraAttachment>();
            List<JiraLink> links = ExtractLinks(issueKey, remoteIssue.SelectTokens("$.fields.issuelinks[*]").Cast<JObject>()) ?? new List<JiraLink>();
            var epicLinkField = jiraProvider.GetSettings().EpicLinkField;

            // save these field since these might be removed in the loop
            string reporter = GetAuthor(fields);
            var createdOn = fields.TryGetValue("created", out object crdate) ? (DateTime)crdate : default(DateTime);
            if (createdOn == DateTime.MinValue)
                Logger.Log(LogLevel.Debug, "created key was not found, using DateTime default value");

            var changelog = jiraProvider.DownloadChangelog(issueKey).OrderByDescending(c => (long)c.SelectToken("$.id")).ToList();
            Logger.Log(LogLevel.Debug, $"Downloaded issue: {issueKey} changelog.");

            Stack<JiraRevision> revisions = new Stack<JiraRevision>();

            foreach (var change in changelog)
            {
                DateTime created = change.ExValue<DateTime>("$.created");
                string author = GetAuthor(change);

                List<RevisionAction<JiraLink>> linkChanges = new List<RevisionAction<JiraLink>>();
                List<RevisionAction<JiraAttachment>> attachmentChanges = new List<RevisionAction<JiraAttachment>>();
                Dictionary<string, object> fieldChanges = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

                var items = change.SelectTokens("$.items[*]")?.Cast<JObject>()?.Select(i => new JiraChangeItem(i));
                foreach (var item in items)
                {
                    switch (item.Field)
                    {
                        case "Epic Link" when !string.IsNullOrWhiteSpace(epicLinkField):
                            HandleCustomFieldChange(item, epicLinkField, fieldChanges, fields);
                            break;
                        case "Parent":
                        case "IssueParentAssociation":
                            HandleCustomFieldChange(item, "parent", fieldChanges, fields);
                            break;
                        case "Link":
                            HandleLinkChange(item, issueKey, jiraProvider, linkChanges, links);
                            break;
                        case "Attachment":
                            HandleAttachmentChange(item, attachmentChanges, attachments);
                            break;
                        default:
                            HandleFieldChange(item, jiraProvider, fieldChanges, fields);
                            break;
                    }
                }

                var revision = new JiraRevision(jiraItem) { Time = created, Author = author, AttachmentActions = attachmentChanges, LinkActions = linkChanges, Fields = fieldChanges };
                revisions.Push(revision);
            }

            // what is left after undoing all changes is first revision
            var attActions = attachments.Select(a => new RevisionAction<JiraAttachment>() { ChangeType = RevisionChangeType.Added, Value = a }).ToList();
            var linkActions = links.Select(l => new RevisionAction<JiraLink>() { ChangeType = RevisionChangeType.Added, Value = l }).ToList();
            var fieldActions = fields;

            var firstRevision = new JiraRevision(jiraItem) { Time = createdOn, Author = reporter, AttachmentActions = attActions, Fields = fieldActions, LinkActions = linkActions };
            revisions.Push(firstRevision);
            var listOfRevisions = revisions.ToList();

            List<JiraRevision> commentRevisions = BuildCommentRevisions(jiraItem, jiraProvider);
            listOfRevisions.AddRange(commentRevisions);

            var settings = jiraProvider.GetSettings();
            if (settings.IncludeCommits)
            {
                if (settings.RepositoryMap == null)
                {
                    Logger.Log(LogLevel.Warning, $"IncludeCommits was 'true' in the config, but no RepositoryMap was specified in the config. " +
                        $"Please add a RepositoryMap in order to migrate git artifact links. Git artifacts will be skipped for now...");
                }
                else
                {
                    var commitRepositories = jiraProvider.GetCommitRepositories(jiraItem.Id);
                    foreach (var respository in commitRepositories)
                    {
                        var commits = respository.SelectTokens(".commits[*]");
                        foreach (JToken commit in commits)
                        {
                            var commitCreatedOn = commit.ExValue<DateTime>("$.authorTimestamp");
                            var commitAuthor = GetAuthor(commit as JObject);
                            var jiraCommit = commit.ToObject<JiraCommit>();
                            var repositoryName = respository.SelectToken("$.name").Value<string>();
                            if (string.IsNullOrEmpty(repositoryName))
                            {
                                continue;
                            }

                            var hasRespositoryTarget = settings.RepositoryMap.Repositories.Exists(r => r.Source == repositoryName && !string.IsNullOrEmpty(r.Target));
                            if (!hasRespositoryTarget)
                            {
                                continue;
                            }

                            jiraCommit.Repository = repositoryName;
                            var commitRevision = new JiraRevision(jiraItem) { Time = commitCreatedOn, Author = commitAuthor, Fields = new Dictionary<string, object>(), Commit = new RevisionAction<JiraCommit>() { ChangeType = RevisionChangeType.Added, Value = jiraCommit } };
                            listOfRevisions.Add(commitRevision);
                        }
                    }
                }
            }

            listOfRevisions.Sort();

            foreach (var revAndI in listOfRevisions.Select((r, i) => (r, i)))
                revAndI.Item1.Index = revAndI.Item2;

            return listOfRevisions;
        }

        private static void HandleCustomFieldChange(JiraChangeItem item, string customFieldName, Dictionary<string, object> fieldChanges, Dictionary<string, object> fields)
        {
            fieldChanges[customFieldName] = item.ToString;

            // undo field change
            if (string.IsNullOrWhiteSpace(item.From))
                fields.Remove(customFieldName);
            else
                fields[customFieldName] = item.FromString;
        }

        private static void HandleFieldChange(JiraChangeItem item, IJiraProvider jiraProvider, Dictionary<string, object> fieldChanges, Dictionary<string, object> fields)
        {
            var (fieldref, from, to) = TransformFieldChange(item, jiraProvider);

            fieldChanges[fieldref] = to;

            // undo field change
            if (string.IsNullOrEmpty(from))
                fields.Remove(fieldref);
            else
                fields[fieldref] = from;
        }

        private static void HandleLinkChange(JiraChangeItem item, string issueKey, IJiraProvider jiraProvider, List<RevisionAction<JiraLink>> linkChanges, List<JiraLink> links)
        {
            var linkChange = TransformLinkChange(item, issueKey, jiraProvider);
            if (linkChange == null)
                return;

            linkChanges.Add(linkChange);
            UndoLinkChange(linkChange, links);
        }

        private static void HandleAttachmentChange(JiraChangeItem item, List<RevisionAction<JiraAttachment>> attachmentChanges, List<JiraAttachment> attachments)
        {
            var attachmentChange = TransformAttachmentChange(item);
            if (attachmentChange == null)
                return;

            if (UndoAttachmentChange(attachmentChange, attachments))
            {
                attachmentChanges.Add(attachmentChange);
            }
            else
            {
                Logger.Log(LogLevel.Debug, $"Attachment {item.ToString ?? item.FromString} cannot be migrated because it was deleted.");
            }
        }

        private static List<JiraRevision> BuildCommentRevisions(JiraItem jiraItem, IJiraProvider jiraProvider)
        {
            var renderedFields = jiraItem.RemoteIssue.SelectToken("$.renderedFields.comment.comments");
            var comments = jiraProvider.GetCommentsByItemKey(jiraItem.Key);
            return comments.Select((c, i) =>
            {
                var rc = renderedFields.SelectToken($"$.[{i}].body");
                return BuildCommentRevision(c, rc, jiraItem);
            }).ToList();
        }

        private static JiraRevision BuildCommentRevision(Comment c, JToken rc, JiraItem jiraItem)
        {
            var author = "NoAuthorDefined";
            if (c.AuthorUser is null)
            {
                Logger.Log(LogLevel.Warning, $"c.AuthorUser is null in comment revision for jiraItem.Key: '{jiraItem.Key}'. Using NoAuthorDefined as author. ");
            }
            else
            {
                if (c.AuthorUser.Username is null)
                {
                    author = GetAuthorIdentityOrDefault(c.AuthorUser.AccountId);
                }
                else
                {
                    author = c.AuthorUser.Username;
                }
            }

            return new JiraRevision(jiraItem)
            {
                Author = author,
                Time = c.CreatedDate.Value,
                Fields = new Dictionary<string, object>() { { "comment", c.Body }, { "comment$Rendered", rc.Value<string>() } },
                AttachmentActions = new List<RevisionAction<JiraAttachment>>(),
                LinkActions = new List<RevisionAction<JiraLink>>()
            };
        }

        private static bool UndoAttachmentChange(RevisionAction<JiraAttachment> attachmentChange, List<JiraAttachment> attachments)
        {
            if (attachmentChange.ChangeType == RevisionChangeType.Removed)
            {
                Logger.Log(LogLevel.Debug, $"Skipping undo for attachment '{attachmentChange.ToString()}'.");
                return false;
            }
            return RemoveAttachment(attachmentChange, attachments);
        }

        private static bool RemoveAttachment(RevisionAction<JiraAttachment> attachmentChange, List<JiraAttachment> attachments)
        {
            var result = attachments.Remove(attachmentChange.Value);
            if (result)
                Logger.Log(LogLevel.Debug, $"Undone attachment '{attachmentChange.ToString()}'.");
            else
                Logger.Log(LogLevel.Debug, $"No attachment to undo for '{attachmentChange.ToString()}'.");
            return result;
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

        private static (string, string, string) TransformFieldChange(JiraChangeItem item, IJiraProvider jira)
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

        private static string GetCustomFieldId(string fieldName, IJiraProvider jira)
        {
            return jira.GetCustomId(fieldName);
        }

        protected static string GetCustomFieldName(string fieldId, IJiraProvider jira)
        {
            var customField = jira.GetCustomField(fieldId);
            if (customField != null)
                return customField.Name;
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
        private static RevisionAction<JiraLink> TransformLinkChange(JiraChangeItem item, string sourceItemKey, IJiraProvider jira)
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
            var linkType = jira.GetLinkType(linkTypeString, targetItemKey);
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
                    Url = attObj.ExValue<string>("$.content")
                };
            }).ToList();
        }

        private static Dictionary<string, Func<JToken, object>> _fieldExtractionMapping = null;
        private static Dictionary<string, object> ExtractFields(string key, JObject remoteIssue, IJiraProvider jira)
        {
            var fields = new Dictionary<string, object>();

            var remoteFields = (JObject)remoteIssue.SelectToken("$.fields");
            var renderedFields = (JObject)remoteIssue.SelectToken("$.renderedFields");

            var extractName = new Func<JToken, object>((t) => t.ExValue<string>("$.name"));
            var extractAccountIdOrUsername = new Func<JToken, object>((t) => t.ExValue<string>("$.name") ?? t.ExValue<string>("$.accountId"));

            if (_fieldExtractionMapping == null)
            {
                _fieldExtractionMapping = new Dictionary<string, Func<JToken, object>>()
                    {
                        { "priority", extractName },
                        { "labels", t => t.Values<string>().Any() ? string.Join(" ", t.Values<string>()) : null },
                        { "assignee", extractAccountIdOrUsername },
                        { "creator", extractAccountIdOrUsername },
                        { "reporter", extractAccountIdOrUsername},
                        { jira.GetSettings().SprintField, t => string.Join(", ", ParseCustomField(jira.GetSettings().SprintField, t, jira)) },
                        { "status", extractName },
                        { "parent", t => t.ExValue<string>("$.key") },
                        { "issuetype", extractName },
                        { "resolution", extractName }
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
                // User picker, server ('name' check) + cloud ('accountId' check)
                else if (type == JTokenType.Object && (prop.Value["accountId"] != null || prop.Value["name"] != null)
                    && prop.Value["emailAddress"] != null && prop.Value["avatarUrls"] != null
                    && prop.Value["displayName"] != null)
                {
                    value = extractAccountIdOrUsername(prop.Value);
                }
                // User picker, on-prem
                else if (type == JTokenType.Object && prop.Value["key"] != null
                    && prop.Value["emailAddress"] != null && prop.Value["avatarUrls"] != null
                    && prop.Value["displayName"] != null)
                {
                    value = prop.Value["key"].ToString();
                }
                else if (prop.Value.Type == JTokenType.Date)
                {
                    value = prop.Value.Value<DateTime>();
                }
                else if (type == JTokenType.Array && prop.Value.Any())
                {
                    value = string.Join(";", prop.Value.Select(st => st.ExValue<string>("$.name")).ToList());
                    if (Regex.Match((string)value, "^[;]+$", RegexOptions.None, TimeSpan.FromMilliseconds(100)).Success || (string)value == "")
                        value = string.Join(";", prop.Value.Select(st => st.ExValue<string>("$.value")).ToList());
                }
                else if (type == Newtonsoft.Json.Linq.JTokenType.Object && prop.Value["value"] != null)
                {
                    value = prop.Value["value"].ToString();
                }

                if (value != null)
                {
                    fields[name] = value;

                    if (renderedFields.TryGetValue(name, out JToken rendered))
                    {
                        if (rendered.Type == JTokenType.String)
                        {
                            fields[name + "$Rendered"] = rendered.Value<string>();
                        }
                        else
                        {
                            Logger.Log(LogLevel.Debug, $"Rendered field {name} contains unparsable type {rendered.Type.ToString()}, using text");
                        }
                    }
                }
            }

            fields["key"] = key;
            fields["issuekey"] = key;

            return fields;
        }
        private static string GetAuthor(Dictionary<string, object> fields)
        {
            var reporter = fields.TryGetValue("reporter", out object rep) ? (string)rep : null;

            return GetAuthorIdentityOrDefault(reporter);
        }
        private static string GetAuthor(JObject change)
        {
            var author = change.ExValue<string>("$.author.name") ?? change.ExValue<string>("$.author.accountId");
            return GetAuthorIdentityOrDefault(author);

        }

        private static string GetAuthorIdentityOrDefault(string author)
        {
            if (string.IsNullOrEmpty(author))
                return default(string);

            return author;

        }

        private static string[] ParseCustomField(string fieldName, JToken value, IJiraProvider provider)
        {
            var serializedValue = new string[] { };
            var customField = provider.GetCustomField(fieldName);
            if (customField != null &&
                provider.GetCustomFieldSerializer(customField.CustomType, out var serializer))
            {
                serializedValue = serializer.FromJson(value);
            }

            return serializedValue;
        }

        #endregion

        private readonly IJiraProvider _provider;

        public string Key { get { return RemoteIssue.ExValue<string>("$.key"); } }
        public string Type { get { return RemoteIssue.ExValue<string>("$.fields.issuetype.name")?.Trim(); } }
        public string Id { get { return RemoteIssue.ExValue<string>("$.id"); } }
        public string EpicParent
        {
            get
            {
                if (!string.IsNullOrEmpty(_provider.GetSettings().EpicLinkField))
                    return RemoteIssue.ExValue<string>($"$.fields.{_provider.GetSettings().EpicLinkField}");
                else
                    return null;
            }
        }
        public string Parent { get { return RemoteIssue.ExValue<string>("$.fields.parent.key"); } }
        public List<string> SubItems { get { return GetSubTasksKey(); } }

        public JObject RemoteIssue { get; private set; }
        public List<JiraRevision> Revisions { get; set; }
        private JiraItem(IJiraProvider provider, JObject remoteIssue)
        {
            this._provider = provider;
            RemoteIssue = remoteIssue;
        }
        internal string GetUserEmail(string author)
        {
            return _provider.GetUserEmail(author);
        }
        internal List<string> GetSubTasksKey()
        {
            return RemoteIssue.SelectTokens("$.fields.subtasks.[*]", false).Select(st => st.ExValue<string>("$.key")).ToList();
        }
    }
}
