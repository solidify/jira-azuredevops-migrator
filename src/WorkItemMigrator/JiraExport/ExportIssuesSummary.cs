using Atlassian.Jira.Remote;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace JiraExport
{
    public class ExportIssuesSummary
    {
        private readonly List<string> _unmappedIssueTypes;
        private readonly Dictionary<string, List<string>> _unmappedIssueStates;
        private readonly List<string> _unmappedUsers;

        public ExportIssuesSummary() {
            _unmappedIssueTypes = new List<string>();
            _unmappedIssueStates = new Dictionary<string, List<string>>();
            _unmappedUsers = new List<string>();
        }

        public void AddUnmappedIssueType(string issueType)
        {
            if (!_unmappedIssueTypes.Contains(issueType))
            {
                _unmappedIssueTypes.Add(issueType);
            }
        }

        public void AddUnmappedIssueState(string issueType, string issueState)
        {
            if (!_unmappedIssueStates.ContainsKey(issueType))
            {
                _unmappedIssueStates.Add(issueType, new List<string>());
            }
            if (!_unmappedIssueStates[issueType].Contains(issueState))
            {
                _unmappedIssueStates[issueType].Add(issueState);
            }
        }

        public void AddUnmappedUser(string user)
        {
            if (!_unmappedUsers.Contains(user))
            {
                _unmappedUsers.Add(user);
            }
        }

        public string GetReportString()
        {
            if(!AnyIssuesFound())
            {
                return "";
            }

            var outSB = new StringBuilder();
            outSB.AppendLine("");
            outSB.AppendLine("################################");
            outSB.AppendLine("### Migration Issues Summary ###");
            outSB.AppendLine("################################");
            if (_unmappedIssueTypes.Count > 0)
            {
                outSB.AppendLine("");
                outSB.AppendLine("### Missing issue type mappings ###");
                outSB.AppendLine("");
                foreach (var issueType in _unmappedIssueTypes)
                {
                    outSB.AppendLine($"- {issueType}");
                }
            }
            if (_unmappedIssueStates.Count > 0)
            {
                outSB.AppendLine("");
                outSB.AppendLine("### Missing status mappings ###");
                outSB.AppendLine("");
                foreach (var issueType in _unmappedIssueStates.Keys)
                {
                    outSB.AppendLine($"- {issueType}");
                    foreach (var status in _unmappedIssueStates[issueType])
                    {
                        outSB.AppendLine($"  - {status}");
                    }
                }
            }
            if (_unmappedUsers.Count > 0)
            {
                outSB.AppendLine("");
                outSB.AppendLine("### Missing user mappings ###");
                outSB.AppendLine("");
                foreach (var user in _unmappedUsers)
                {
                    outSB.AppendLine($"- {user}");
                }
            }

            outSB.AppendLine("");
            outSB.AppendLine("Please fix the above issues and rerun the migration.");

            return outSB.ToString();
        }

        private bool AnyIssuesFound()
        {
            return
                _unmappedIssueTypes.Any()
                || _unmappedIssueStates.Any()
                || _unmappedUsers.Any();
        }
    }
}
