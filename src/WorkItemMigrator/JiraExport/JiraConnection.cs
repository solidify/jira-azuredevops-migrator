using Atlassian.Jira;
using JiraExport;
using Migration.Common.Log;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace JiraExport
{
    public class JiraConnection
    {
        public Jira Initialize(JiraSettings settings)
        {
            return ConnectToJira(settings);
        }

        private Jira ConnectToJira(JiraSettings jiraSettings)
        {
            Jira jira = null;

            try
            {
                Logger.Log(LogLevel.Info, "Connecting to Jira...");

                jira = Jira.CreateRestClient(jiraSettings.Url, jiraSettings.UserID, jiraSettings.Pass);
                jira.RestClient.RestSharpClient.AddDefaultHeader("X-Atlassian-Token", "no-check");
                if (jiraSettings.UsingJiraCloud)
                    jira.RestClient.Settings.EnableUserPrivacyMode = true;

            }
            catch (Exception ex)
            {
                Logger.Log(ex, "Could not connect to Jira!", LogLevel.Critical);
            }

            return jira;
        }
    }
}
