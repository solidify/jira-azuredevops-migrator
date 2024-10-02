using Atlassian.Jira;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static JiraExport.JiraProvider;

namespace JiraExport
{
    public interface IJiraProvider
    {
        JiraSettings GetSettings();

        JObject DownloadIssue(string key);

        IEnumerable<JiraItem> EnumerateIssues(string jql, HashSet<string> skipList, DownloadOptions downloadOptions);

        IEnumerable<JObject> DownloadChangelog(string issueKey);

        string GetUserEmail(string usernameOrAccountId);

        IssueLinkType GetLinkType(string linkTypeString, string targetItemKey, out bool isInwardLink);

        IEnumerable<Comment> GetCommentsByItemKey(string itemKey);

        CustomField GetCustomField(string fieldName);

        bool GetCustomFieldSerializer(string customType, out ICustomFieldValueSerializer serializer);

        string GetCustomId(string propertyName);
        Task<List<RevisionAction<JiraAttachment>>> DownloadAttachments(JiraRevision rev);

        IEnumerable<JObject> GetCommitRepositories(string issueId);
    }
}
