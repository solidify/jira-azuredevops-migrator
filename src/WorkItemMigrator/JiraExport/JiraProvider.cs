using Atlassian.Jira;
using Migration.Common;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JiraExport
{
    public class JiraProvider
    {
        [Flags]
        public enum DownloadOptions
        {
            IncludeParentEpics,
            IncludeEpicChildren,
            IncludeParents,
            IncludeSubItems,
            IncludeLinkedItems
        }

        readonly Dictionary<string, string> _userEmailCache = new Dictionary<string, string>();

        public Jira Jira { get; private set; }

        public JiraSettings Settings { get; private set; }

        public IEnumerable<IssueLinkType> LinkTypes { get; private set; }

        private JiraProvider()
        {
        }

        public static JiraProvider Initialize(JiraSettings settings)
        {
            var provider = new JiraProvider();

            var sprintId = settings.SprintField;
            var storyPointsId = settings.StoryPointsField;

            provider.Jira = ConnectToJira(settings);

            settings.EpicLinkField = provider.GetCustomId("Epic Link");
            settings.SprintField = provider.GetCustomId(sprintId);
            settings.StoryPointsField = provider.GetCustomId(storyPointsId);

            provider.Settings = settings;

            Logger.Log(LogLevel.Info, "Gathering project info...");

            // ensure that Custom fields cache is full
            provider.Jira.Fields.GetCustomFieldsAsync().Wait();
            Logger.Log(LogLevel.Info, "Custom field cache set up.");
            Logger.Log(LogLevel.Info, "Custom parsers set up.");

            provider.LinkTypes = provider.Jira.Links.GetLinkTypesAsync().Result;
            Logger.Log(LogLevel.Info, "Link types cache set up.");

            return provider;
        }

        private static Jira ConnectToJira(JiraSettings jiraSettings)
        {
            Logger.Log(LogLevel.Debug, "Connecting to Jira...");
            Jira jira = null;

            try
            {
                jira = Jira.CreateRestClient(jiraSettings.Url, jiraSettings.UserID, jiraSettings.Pass);
                Logger.Log(LogLevel.Info, "Connected to Jira.");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Critical, $"Could not connect to Jira! Message: {ex.Message}");
            }

            return jira;
        }

        private JiraItem ProcessItem(string issueKey, HashSet<string> skipList, string successMessage)
        {
            var issue = JiraItem.CreateFromRest(issueKey, this);
            Logger.Log(LogLevel.Info, $"Downloaded {issueKey} - {successMessage}");
            skipList.Add(issue.Key);
            return issue;
        }

        private async Task<JiraAttachment> GetAttachmentInfo(string id)
        {
            Logger.Log(LogLevel.Debug, $"Downloading attachment info for attachment {id}");

            try
            {
                var response = await Jira.RestClient.ExecuteRequestAsync(RestSharp.Method.GET, $"rest/api/2/attachment/{id}");
                var attObj = (JObject)response;

                return new JiraAttachment()
                {
                    Id = id,
                    Filename = attObj.ExValue<string>("$.filename"),
                    Url = attObj.ExValue<string>("$.content"),
                    ThumbUrl = attObj.ExValue<string>("$.thumbnail")
                };
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Warning, $"Cannot find info for attachment {id}. Skipping.");
                Logger.Log(ex);
                return null;
            }
        }

        private async Task<JiraAttachment> DownloadAttachmentAsync(JiraAttachment att, WebClientWrapper web)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(att.Url))
                    att = await GetAttachmentInfo(att.Id);

                if (att != null && !string.IsNullOrWhiteSpace(att.Url))
                {
                    string path = Path.Combine(Settings.AttachmentsDir, att.Id, att.Filename);
                    EnsurePath(path);
                    await web.DownloadWithAuthenticationAsync(att.Url, path);
                    att.LocalPath = path;
                    Logger.Log(LogLevel.Debug, $"Downloaded attachment {att.ToString()}");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Warning, $"Attachment download failed. Message: {ex.Message}");
            }

            if (att != null && !string.IsNullOrWhiteSpace(att.ThumbUrl))
            {
                try
                {
                    string thumbname = Path.GetFileNameWithoutExtension(att.Filename) + ".thumb" + Path.GetExtension(att.Filename);
                    var thumbPath = Path.Combine(Settings.AttachmentsDir, att.Id, thumbname);
                    EnsurePath(thumbPath);
                    await web.DownloadWithAuthenticationAsync(att.ThumbUrl, Path.Combine(Settings.AttachmentsDir, att.Id, thumbname));
                    att.LocalThumbPath = thumbPath;
                    Logger.Log(LogLevel.Debug, $"Downloaded attachment thumbnail {att.ToString()}");
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Warning, $"Attachment thumbnail ({att.ToString()}) download failed. Message: {ex.Message}");
                }
            }

            return att;
        }

        private void EnsurePath(string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
            {
                var parentDir = Path.GetDirectoryName(dir);
                EnsurePath(parentDir);
                Directory.CreateDirectory(dir);
            }
        }

        public IEnumerable<JiraItem> EnumerateIssues(string jql, HashSet<string> skipList, DownloadOptions downloadOptions)
        {
            Logger.Log(LogLevel.Info, "Processing issues...");
            int currentStart = 0;
            IEnumerable<string> remoteIssueBatch = null;
            int index = 0;
            do
            {
                var response = Jira.RestClient.ExecuteRequestAsync(RestSharp.Method.GET,
                    $"rest/api/2/search?jql={jql}&startAt={currentStart}&maxResults={Settings.BatchSize}&fields=key").Result;

                remoteIssueBatch = response.SelectTokens("$.issues[*]").OfType<JObject>()
                                           .Select(i => i.SelectToken("$.key").Value<string>());

                currentStart += Settings.BatchSize;

                int totalItems = (int)response.SelectToken("$.total");

                foreach (var issueKey in remoteIssueBatch)
                {
                    if (skipList.Contains(issueKey))
                    {
                        Logger.Log(LogLevel.Info, $"Skipped {issueKey} - already downloaded [{index + 1}/{totalItems}]");
                        index++;
                        continue;
                    }


                    var issue = ProcessItem(issueKey, skipList, $"[{index + 1}/{totalItems}]");
                    yield return issue;
                    index++;

                    if (downloadOptions.HasFlag(DownloadOptions.IncludeParentEpics) && issue.EpicParent != null && !skipList.Contains(issue.EpicParent))
                    {
                        var parentEpic = ProcessItem(issue.EpicParent, skipList, $"epic parent of {issueKey}");
                        yield return parentEpic;
                    }

                    if (downloadOptions.HasFlag(DownloadOptions.IncludeParents) && issue.Parent != null && !skipList.Contains(issue.EpicParent))
                    {
                        var parent = ProcessItem(issue.Parent, skipList, $"parent of {issueKey}");
                        yield return parent;
                    }

                    if (downloadOptions.HasFlag(DownloadOptions.IncludeSubItems) && issue.SubItems != null && issue.SubItems.Any())
                    {
                        foreach (var subitemKey in issue.SubItems)
                        {
                            if (!skipList.Contains(subitemKey))
                            {
                                var subItem = ProcessItem(subitemKey, skipList, $"sub-item of {issueKey}");
                                yield return subItem;
                            }
                        }
                    }
                }
            }
            while (remoteIssueBatch != null && remoteIssueBatch.Any());
        }

        public IEnumerable<JObject> DownloadChangelog(string issueKey)
        {
            bool isLast = true;
            int batchSize = 100;
            int currentStart = 0;
            do
            {
                var response = (JObject)Jira.RestClient.ExecuteRequestAsync(RestSharp.Method.GET,
                    $"rest/api/2/issue/{issueKey}/changelog?maxResults={batchSize}&startAt={currentStart}").Result;

                currentStart += batchSize;
                isLast = (bool)response.SelectToken("$.isLast");

                var changes = response.SelectTokens("$.values[*]").Cast<JObject>();
                foreach (var change in changes)
                    yield return change;

            } while (!isLast);
        }

        public JObject DownloadIssue(string key)
        {
            var response = Jira.RestClient.ExecuteRequestAsync(RestSharp.Method.GET, $"rest/api/2/issue/{key}?expand=renderedFields").Result;
            var remoteItem = (JObject)response;
            return remoteItem;
        }

        public async Task<List<RevisionAction<JiraAttachment>>> DownloadAttachments(JiraRevision rev)
        {
            var attChanges = rev.AttachmentActions;

            if (attChanges != null && attChanges.Any(a => a.ChangeType == RevisionChangeType.Added))
            {
                var downloadedAtts = new List<JiraAttachment>();
                using (var web = new WebClientWrapper(this))
                {
                    foreach (var remoteAtt in attChanges)
                    {
                        var jiraAtt = await DownloadAttachmentAsync(remoteAtt.Value, web);
                        if (jiraAtt != null && !string.IsNullOrWhiteSpace(jiraAtt.LocalPath))
                        {
                            Logger.Log(LogLevel.Info, $"Downloaded {jiraAtt.ToString()} to {jiraAtt.LocalPath}");
                            downloadedAtts.Add(jiraAtt);
                        }
                    }
                }

                // of added attachments, leave only attachments that have been successfully downloaded
                attChanges.RemoveAll(ac => ac.ChangeType == RevisionChangeType.Added);
                attChanges.AddRange(downloadedAtts.Select(da => new RevisionAction<JiraAttachment>() { ChangeType = RevisionChangeType.Added, Value = da }));
            }

            return attChanges;
        }

        public int GetNumberOfComments(string key)
        {
            return Jira.Issues.GetCommentsAsync(key).Result.Count();
        }

        public string GetUserEmail(string username)
        {
            if (_userEmailCache.TryGetValue(username, out string email))
                return email;
            else
            {
                var user = Jira.Users.GetUserAsync(username).Result;
                email = user.Email;
                _userEmailCache.Add(username, email);
                return email;
            }
        }

        public string GetCustomId(string propertyName)
        {
            var customId = string.Empty;
            var response = (JArray)Jira.RestClient.ExecuteRequestAsync(RestSharp.Method.GET, $"rest/api/2/field").Result;
            foreach (var item in response)
            {
                var nameField = (JValue)item.SelectToken("name");
                if (nameField.Value.ToString() == propertyName)
                {
                    var idField = (JValue)item.SelectToken("id");
                    customId = idField.Value.ToString();
                }
            }
            return customId;
        }
    }
}
