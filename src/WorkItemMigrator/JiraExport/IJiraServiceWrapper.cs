using Atlassian.Jira;
using Atlassian.Jira.Remote;

namespace JiraExport
{
    public interface IJiraServiceWrapper
    {
        IIssueFieldService Fields { get; }
        IIssueService Issues { get; }
        IIssueLinkService Links { get; }
        IJiraRestClient RestClient { get; }
        IJiraUserService Users { get; }
    }
}
