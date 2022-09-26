using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Atlassian.Jira;
using Atlassian.Jira.Remote;
using Migration.Common;
using Migration.Common.Log;

using Newtonsoft.Json.Linq;

using RestSharp;

namespace JiraExport
{
    public class JiraProvider : IJiraProvider
    {
        [Flags]
        public enum DownloadOptions
        {
            None = 0,
            IncludeParentEpics = 1,
            IncludeParents = 2,
            IncludeSubItems = 4
        }

        private readonly string JiraApiV2 = "rest/api/2";

        private ILookup<string, string> JiraNameFieldCache = null;

        private ILookup<string, string> JiraKeyFieldCache = null;

        readonly Dictionary<string, string> _userEmailCache = new Dictionary<string, string>();

        private readonly IJiraServiceWrapper _jiraServiceWrapper;

        public JiraSettings Settings { get; private set; }

        public IEnumerable<IssueLinkType> LinkTypes { get; private set; }

        public JiraProvider(IJiraServiceWrapper jiraServiceWrapper)
        {
            _jiraServiceWrapper = jiraServiceWrapper;
        }

        public void Initialize(JiraSettings settings)
        {
            Settings = settings;

            Logger.Log(LogLevel.Info, "Retrieving Jira fields...");
            try
            {
                _jiraServiceWrapper.Fields.GetCustomFieldsAsync().Wait();
            }
            catch (Exception e)
            {
                Logger.Log(e, "Failed to retrieve fields from Jira");
            }

            Logger.Log(LogLevel.Info, "Retrieving Jira link types...");
            try
            {
                LinkTypes = _jiraServiceWrapper.Links.GetLinkTypesAsync().Result;
            }

            catch (Exception e)
            {
                Logger.Log(e, "Failed to retrieve linktypes from Jira");
            }
        }

        public JiraSettings GetSettings()
        {
            return Settings;
        }

        public IssueLinkType GetLinkType(string linkTypeString, string targetItemKey)
        {
            return LinkTypes.FirstOrDefault(lt => linkTypeString.EndsWith(lt.Outward + " " + targetItemKey));
        }

        public IEnumerable<Comment> GetCommentsByItemKey(string itemKey)
        {
            return _jiraServiceWrapper.Issues.GetCommentsAsync(itemKey).Result;
        }

        public bool GetCustomField(string fieldName, out CustomField customField)
        {
            bool found = _jiraServiceWrapper.RestClient.Settings.Cache.CustomFields.TryGetValue(fieldName, out CustomField cF);
            customField = cF;
            return found;
        }

        public bool GetCustomFieldSerializer(string customType, out ICustomFieldValueSerializer serializer)
        {
            bool found = _jiraServiceWrapper.RestClient.Settings.CustomFieldSerializers.TryGetValue(customType, out ICustomFieldValueSerializer s);
            serializer = s;
            return found;
        }

        private JiraItem ProcessItem(string issueKey, HashSet<string> skipList)
        {
            var issue = JiraItem.CreateFromRest(issueKey, this);
            if (issue == null)
                return default(JiraItem);

            skipList.Add(issue.Key);
            return issue;
        }

        private async Task<JiraAttachment> GetAttachmentInfo(string id)
        {
            Logger.Log(LogLevel.Debug, $"Downloading attachment info for attachment '{id}'.");

            try
            {
                var response = await _jiraServiceWrapper.RestClient.ExecuteRequestAsync(Method.GET, $"{JiraApiV2}/attachment/{id}");
                var attObj = (JObject)response;

                return new JiraAttachment
                {
                    Id = id,
                    Filename = attObj.ExValue<string>("$.filename"),
                    Url = attObj.ExValue<string>("$.content")
                };
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Warning, $"Cannot find info for attachment '{id}', skipping. Reason '{ex.Message}'.");
                return null;
            }
        }

        private async Task<JiraAttachment> DownloadAttachmentAsync(JiraAttachment att)
        {
            if (att != null)
            {
                if (string.IsNullOrWhiteSpace(att.Url))
                    att = await GetAttachmentInfo(att.Id);

                if (att != null && !string.IsNullOrWhiteSpace(att.Url))
                {
                    try
                    {
                        var path = Path.Combine(Settings.AttachmentsDir, att.Id, att.Filename);
                        EnsurePath(path);

                        await DownloadWithJiraRestClientAsync(att.Url, path);

                        att.LocalPath = path;
                        Logger.Log(LogLevel.Debug, $"Downloaded attachment '{att}'");
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogLevel.Warning, $"Attachment download failed for '{att.Id}'. Reason '{ex.Message}'.");
                    }
                }
            }

            return att;
        }

        private async Task DownloadWithJiraRestClientAsync(string url, string fileFullPath)
        {
            var attachmentData = _jiraServiceWrapper.RestClient.DownloadData(url);

            using (var stream = new MemoryStream(attachmentData))
            {
                using (var file = File.Create(fileFullPath))
                {
                    await stream.CopyToAsync(file);
                }
            }
        }

        private void EnsurePath(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            var dir = Path.GetDirectoryName(path);
            Directory.CreateDirectory(dir);
        }

        public IEnumerable<JiraItem> EnumerateIssues(string jql, HashSet<string> skipList, DownloadOptions downloadOptions)
        {
            var currentStart = 0;
            IEnumerable<string> remoteIssueBatch = null;
            var index = 0;

            Logger.Log(LogLevel.Debug, "Enumerate remote issues");

            do
            {
                JToken response = null;
                try
                {
                    response = _jiraServiceWrapper.RestClient.ExecuteRequestAsync(Method.GET, $"{JiraApiV2}/search?jql={jql}&startAt={currentStart}&maxResults={Settings.BatchSize}&fields=key").Result;
                }
                catch (Exception e)
                {
                    Logger.Log(e, "Failed to retrieve issues");
                    break;
                }
                if (response != null)
                {
                    remoteIssueBatch = response?.SelectTokens("$.issues[*]").OfType<JObject>()
                                            .Select(i => i.SelectToken("$.key").Value<string>());

                    if (remoteIssueBatch == null)
                    {
                        Logger.Log(LogLevel.Warning, $"No issuse were found using jql: {jql}");
                        break;
                    }

                    currentStart += Settings.BatchSize;

                    int totalItems = (int)response.SelectToken("$.total");

                    foreach (var issueKey in remoteIssueBatch)
                    {
                        if (skipList.Contains(issueKey))
                        {
                            Logger.Log(LogLevel.Info, $"Skipped Jira '{issueKey}' - already downloaded.");
                            index++;
                            continue;
                        }

                        Logger.Log(LogLevel.Info, $"Processing {index + 1}/{totalItems} - '{issueKey}'.");
                        var issue = ProcessItem(issueKey, skipList);

                        if (issue == null)
                            continue;

                        yield return issue;
                        index++;

                        if (downloadOptions.HasFlag(DownloadOptions.IncludeParentEpics) && (issue.EpicParent != null) && !skipList.Contains(issue.EpicParent))
                        {
                            Logger.Log(LogLevel.Info, $"Processing epic parent '{issue.EpicParent}'.");
                            var parentEpic = ProcessItem(issue.EpicParent, skipList);
                            yield return parentEpic;
                        }

                        if (downloadOptions.HasFlag(DownloadOptions.IncludeParents) && (issue.Parent != null) && !skipList.Contains(issue.Parent))
                        {
                            Logger.Log(LogLevel.Info, $"Processing parent issue '{issue.Parent}'.");
                            var parent = ProcessItem(issue.Parent, skipList);
                            yield return parent;
                        }

                        if (downloadOptions.HasFlag(DownloadOptions.IncludeSubItems) && (issue.SubItems != null) && issue.SubItems.Any())
                        {
                            foreach (var subitemKey in issue.SubItems)
                            {
                                if (!skipList.Contains(subitemKey))
                                {
                                    Logger.Log(LogLevel.Info, $"Processing sub-item '{subitemKey}'.");
                                    var subItem = ProcessItem(subitemKey, skipList);
                                    yield return subItem;
                                }
                            }
                        }
                    }
                }
            }
            while (remoteIssueBatch != null && remoteIssueBatch.Any());
        }

        public struct JiraVersion
        {
            public string Version { get; set; }
            public string DeploymentType { get; set; }

            public JiraVersion(string version, string deploymentType)
            {
                Version = version;
                DeploymentType = deploymentType;
            }
        }

        public int GetItemCount(string jql)
        {
            Logger.Log(LogLevel.Debug, $"Get item count using query: '{jql}'");
            try
            {
                var response = _jiraServiceWrapper.RestClient.ExecuteRequestAsync(Method.GET, $"{JiraApiV2}/search?jql={jql}&maxResults=0").Result;

                return (int)response.SelectToken("$.total");
            }
            catch (Exception e)
            {
                Logger.Log(e, $"Failed to get item count using query: '{jql}'");
                return 0;
            }

        }

        public JiraVersion GetJiraVersion()
        {
            var response = (JObject)_jiraServiceWrapper.RestClient.ExecuteRequestAsync(Method.GET, $"{JiraApiV2}/serverInfo").Result;
            return new JiraVersion((string)response.SelectToken("$.version"), (string)response.SelectToken("$.deploymentType"));
        }

        public IEnumerable<JObject> DownloadChangelog(string issueKey)
        {
            var response = (JObject)_jiraServiceWrapper.RestClient.ExecuteRequestAsync(Method.GET, $"{JiraApiV2}/issue/{issueKey}?expand=changelog,renderedFields&fields=created").Result;
            return response.SelectTokens("$.changelog.histories[*]").Cast<JObject>();
        }

        public JObject DownloadIssue(string key)
        {
            try
            {
                var response =
                    _jiraServiceWrapper.RestClient.ExecuteRequestAsync(Method.GET, $"{JiraApiV2}/issue/{key}?expand=renderedFields").Result;

                var remoteItem = (JObject)response;
                return remoteItem;
            }
            catch (Exception e)
            {
                Logger.Log(e, $"Failed to download issue with key: {key}");
                return default(JObject);
            }

        }

        public async Task<List<RevisionAction<JiraAttachment>>> DownloadAttachments(JiraRevision rev)
        {
            var attChanges = rev.AttachmentActions;

            if (attChanges != null && attChanges.Any(a => a.ChangeType == RevisionChangeType.Added))
            {
                var downloadedAtts = new List<JiraAttachment>();

                foreach (var remoteAtt in attChanges)
                {
                    var jiraAtt = await DownloadAttachmentAsync(remoteAtt.Value);
                    if (jiraAtt != null && !string.IsNullOrWhiteSpace(jiraAtt.LocalPath))
                    {
                        downloadedAtts.Add(jiraAtt);
                    }
                }

                // of added attachments, leave only attachments that have been successfully downloaded
                attChanges.RemoveAll(ac => ac.ChangeType == RevisionChangeType.Added);
                attChanges.AddRange(downloadedAtts.Select(da => new RevisionAction<JiraAttachment> { ChangeType = RevisionChangeType.Added, Value = da }));
            }

            return attChanges;
        }

        public int GetNumberOfComments(string key)
        {
            return _jiraServiceWrapper.Issues.GetCommentsAsync(key).Result.Count();
        }

        public string GetUserEmail(string usernameOrAccountId)
        {
            if (_userEmailCache.TryGetValue(usernameOrAccountId, out string email))
            {
                return email;
            }
            try
            {
                var user = _jiraServiceWrapper.Users.GetUserAsync(usernameOrAccountId).Result;
                var isUserEmailMissing = string.IsNullOrEmpty(user.Email);
                if (isUserEmailMissing)
                {
                    Logger.Log(LogLevel.Warning,
                        Settings.UsingJiraCloud
                            ? $"Email is not public for user '{usernameOrAccountId}' in Jira, using usernameOrAccountId '{usernameOrAccountId}' for mapping."
                            : $"Email for user '{usernameOrAccountId}' not found in Jira, using username '{usernameOrAccountId}' for mapping.");
                }
                email = isUserEmailMissing ? usernameOrAccountId : user.Email;
                _userEmailCache.Add(usernameOrAccountId, email);
                return email;
            }
            catch (Exception)
            {
                Logger.Log(LogLevel.Warning,
                    Settings.UsingJiraCloud
                        ? $"Specified user '{usernameOrAccountId}' does not exist or you do not have required permissions, using accountId '{usernameOrAccountId}'"
                        : $"User '{usernameOrAccountId}' not found in Jira, using username '{usernameOrAccountId}' for mapping.");
                _userEmailCache.Add(usernameOrAccountId, usernameOrAccountId);
                return usernameOrAccountId;
            }
        }

        public string GetCustomId(string propertyName)
        {   
            var customId = string.Empty;
            JArray response = null;

            if (JiraNameFieldCache == null)
            {
                response = (JArray)_jiraServiceWrapper.RestClient.ExecuteRequestAsync(Method.GET, $"{JiraApiV2}/field").Result;
                JiraNameFieldCache = CreateFieldCacheLookup(response, "name", "id");
            }

            customId = GetItemFromFieldCache(propertyName, JiraNameFieldCache);

            if (string.IsNullOrEmpty(customId))
            {
                if (JiraKeyFieldCache == null)
                {
                    response = response ?? (JArray)_jiraServiceWrapper.RestClient.ExecuteRequestAsync(Method.GET, $"{JiraApiV2}/field").Result;
                    JiraKeyFieldCache = CreateFieldCacheLookup(response, "key", "id");
                }
                customId = GetItemFromFieldCache(propertyName, JiraKeyFieldCache);
            }

            return customId;
        }

        private ILookup<string, string> CreateFieldCacheLookup(JArray response, string key, string value)
        {
            return response
                .Where(field => field.Value<string>(key) != null && field.Value<string>(value) != null)
                .Select(field => new { key = field.Value<string>(key).ToLower(), value = field.Value<string>(value).ToLower() })
                .ToLookup(l => l.key, l => l.value);
        }

        private string GetItemFromFieldCache(string propertyName, ILookup<string,string> cache)
        {
            string customId = null;
            var query = cache.FirstOrDefault(x => x.Key.Equals(propertyName.ToLower()));
            if (query != null)
            {
                customId = query.Any() ? query.First() : null;
                if (query.Count() > 1)
                {
                    Logger.Log(LogLevel.Warning, $"Multiple fields found for {propertyName}. Selecting {customId}.");
                }
            }
            return customId;
        }
    }
}
