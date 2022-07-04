using NUnit.Framework;

using JiraExport;
using AutoFixture.AutoNSubstitute;
using AutoFixture;

namespace Migration.Jira_Export.Tests
{
    [TestFixture]
    public class JiraLinkTests
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
        public void When_calling_to_string_Then_the_expected_string_value_is_returned()
        {
            JiraLink sut = new JiraLink();
            sut.LinkType = "System.LinkTypes.Hierarchy-Forward";
            sut.SourceItem = "sourceItem";
            sut.TargetItem = "targetItem";

            string expectedToString = $"[{sut.LinkType}] {sut.SourceItem}->{sut.TargetItem}";

            Assert.That(() => sut.ToString(), Is.EqualTo(expectedToString));
        }

        [Test]
        public void When_calling_equals_with_two_equal_jira_attachments_Then_true_is_returned()
        {
            JiraLink sut1 = new JiraLink();
            sut1.LinkType = "System.LinkTypes.Hierarchy-forward";
            sut1.SourceItem = "SourceItem";
            sut1.TargetItem = "TargetItem";

            JiraLink sut2 = new JiraLink();
            sut2.LinkType = "System.LinkTypes.Hierarchy-forward";
            sut2.SourceItem = "SourceItem";
            sut2.TargetItem = "TargetItem";

            Assert.That(() => sut1.Equals(sut2), Is.True);
        }
    }
}