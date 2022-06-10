using NUnit.Framework;

using JiraExport;
using AutoFixture.AutoNSubstitute;
using AutoFixture;

namespace Migration.Jira_Export.Tests
{
    [TestFixture]
    public class JiraLinkTests
    {
        // use auto fixiture to help mock and instantiate with dummy data with nsubsitute. 
        private Fixture _fixture;

        [SetUp]
        public void Setup()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoNSubstituteCustomization() { });
        }

        [Test]
        public void When_calling_to_string_Then_the_expected_string_value_is_returned()
        {
            JiraLink jiraLink = new JiraLink();
            jiraLink.LinkType = "System.LinkTypes.Hierarchy-Forward";
            jiraLink.SourceItem = "sourceItem";
            jiraLink.TargetItem = "targetItem";

            string expectedToString = "[System.LinkTypes.Hierarchy-Forward] sourceItem->targetItem";

            Assert.That(() => jiraLink.ToString(), Is.EqualTo(expectedToString));
        }

        [Test]
        public void When_calling_execute_with_args_Then_run_is_executed()
        {
            JiraLink jiraLink1 = new JiraLink();
            jiraLink1.LinkType = "System.LinkTypes.Hierarchy-forward";
            jiraLink1.SourceItem = "SourceItem";
            jiraLink1.TargetItem = "TargetItem";

            JiraLink jiraLink2 = new JiraLink();
            jiraLink2.LinkType = "System.LinkTypes.Hierarchy-Forward";
            jiraLink2.SourceItem = "sourceItem";
            jiraLink2.TargetItem = "targetItem";

            Assert.That(() => jiraLink1.Equals(jiraLink2), Is.True);
        }
    }
}