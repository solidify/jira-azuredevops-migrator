using AutoFixture;
using AutoFixture.AutoNSubstitute;
using JiraExport;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NUnit.Framework;
using System.Diagnostics.CodeAnalysis;

namespace Migration.Jira_Export.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class JiraRevisionTests
    {
        // use auto fixture to help mock and instantiate with dummy data with nsubsitute. 
        private Fixture _fixture;

        [SetUp]
        public void Setup()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoNSubstituteCustomization() { });
        }

        [Test]
        public void When_calling_compare_to_with_null_argumentss_Then_1_is_returned()
        {
            JiraRevision sut1 = new JiraRevision(CreateJiraItem());
            Assert.That(() => sut1.CompareTo(null), Is.EqualTo(1));
        }

        [Test]
        public void When_calling_compare_to_with_equal_objects_Then_0_is_returned()
        {
            JiraRevision sut1 = new JiraRevision(CreateJiraItem());
            JiraRevision sut2 = new JiraRevision(CreateJiraItem());

            Assert.That(() => sut1.CompareTo(sut2), Is.EqualTo(0));
        }

        [Test]
        public void When_calling_compare_to_with_non_equal_objects_Then_1_is_returned()
        {
            JiraRevision sut1 = new JiraRevision(CreateJiraItem());
            JiraRevision sut2 = new JiraRevision(CreateJiraItem());
            sut1.Time = System.DateTime.Now;

            Assert.That(() => sut1.CompareTo(sut2), Is.EqualTo(1));
        }

        private JiraItem CreateJiraItem()
        {
            var provider = _fixture.Freeze<IJiraProvider>();

            var issueType = JObject.Parse(@"{ 'issuetype': {'name': 'Story'}}");
            var renderedFields = JObject.Parse("{ 'custom_field_name': 'SomeValue', 'description': 'RenderedDescription' }");
            string issueKey = "issue_key";

            JObject remoteIssue = new JObject
            {
                { "fields", issueType },
                { "renderedFields", renderedFields },
                { "key", issueKey }
            };

            provider.DownloadIssue(default).ReturnsForAnyArgs(remoteIssue);
            provider.GetSettings().ReturnsForAnyArgs(CreateJiraSettings());

            JiraItem jiraItem = JiraItem.CreateFromRest(issueKey, provider);

            return jiraItem;
        }

        private JiraSettings CreateJiraSettings()
        {
            JiraSettings settings = new JiraSettings("userID", "pass", "token", "url", "project")
            {
                EpicLinkField = "EpicLinkField",
                SprintField = "SprintField"
            };

            return settings;
        }
    }
}