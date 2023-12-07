using Atlassian.Jira;
using Atlassian.Jira.Remote;
using JiraExport;
using Migration.Common.Log;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Text;

namespace JiraExport
{
    public class JiraServiceWrapper : IJiraServiceWrapper
    {
        private readonly Jira _jira;

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

                if (!string.IsNullOrWhiteSpace(jiraSettings.Token))
                    _jira.RestClient.RestSharpClient.Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(jiraSettings.Token, "Bearer");

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
