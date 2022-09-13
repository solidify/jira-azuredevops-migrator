using Atlassian.Jira;
using Atlassian.Jira.Remote;
using JiraExport;
using Migration.Common.Log;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace JiraExport
{
    public class JiraServiceWrapper : IJiraServiceWrapper
    {
        private Jira _jira;

        public IIssueFieldService Fields => _jira.Fields;
        public IIssueService Issues => _jira.Issues;
        public IIssueLinkService Links => _jira.Links;
        public IJiraRestClient RestClient => _jira.RestClient;
        public IJiraUserService Users => _jira.Users;

        public JiraServiceWrapper(JiraSettings jiraSettings)
        {
            try
            {
                Logger.Log(LogLevel.Info, "Connecting to Jira...");

                _jira = Jira.CreateRestClient(jiraSettings.Url, jiraSettings.UserID, jiraSettings.Pass);
                _jira.RestClient.RestSharpClient.AddDefaultHeader("X-Atlassian-Token", "no-check");
                if (jiraSettings.UsingJiraCloud)
                    _jira.RestClient.Settings.EnableUserPrivacyMode = true;
            }
            catch (Exception ex)
            {
                Logger.Log(ex, "Could not connect to Jira!", LogLevel.Critical);
            }
        }
    }
}
