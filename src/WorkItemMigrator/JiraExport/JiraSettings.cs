
using Migration.Common.Config;
using Newtonsoft.Json.Linq;

namespace JiraExport
{
    public class JiraSettings
    {
        public string UserID { get; private set; }
        public string Pass { get; private set; }
        public string Token { get; private set; }
        public string Url { get; private set; }
        public string Project { get; set; }
        public string EpicLinkField { get; set; }
        public string SprintField { get; set; }
        public string UserMappingFile { get; set; }
        public int BatchSize { get; set; }
        public string AttachmentsDir { get; set; }
        public string JQL { get; set; }
        public bool UsingJiraCloud { get; set; }
        public bool IncludeDevelopmentLinks { get; set; }
        public RepositoryMap RepositoryMap { get; set; }

        public JiraSettings(string userID, string pass, string token, string url, string project)
        {
            UserID = userID;
            Pass = pass;
            Token = token;
            Url = url;
            Project = project;
        }
    }
}