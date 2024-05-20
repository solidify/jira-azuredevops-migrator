using AutoFixture;
using AutoFixture.AutoNSubstitute;
using JiraExport;
using NUnit.Framework;
using System.Diagnostics.CodeAnalysis;

namespace Migration.Jira_Export.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
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
            JiraLink sut = new JiraLink
            {
                LinkType = "System.LinkTypes.Hierarchy-Forward",
                SourceItem = "sourceItem",
                TargetItem = "targetItem"
            };

            string expectedToString = $"[{sut.LinkType}] {sut.SourceItem}->{sut.TargetItem}";

            Assert.That(() => sut.ToString(), Is.EqualTo(expectedToString));
        }

        [Test]
        public void When_calling_equals_with_two_equal_jira_attachments_Then_true_is_returned()
        {
            JiraLink sut1 = new JiraLink
            {
                LinkType = "System.LinkTypes.Hierarchy-forward",
                SourceItem = "SourceItem",
                TargetItem = "TargetItem"
            };

            JiraLink sut2 = new JiraLink
            {
                LinkType = "System.LinkTypes.Hierarchy-forward",
                SourceItem = "SourceItem",
                TargetItem = "TargetItem"
            };

            Assert.That(() => sut1.Equals(sut2), Is.True);
        }

        [Test]
        public void When_calling_equals_with_null_argumentss_Then_false_is_returned()
        {
            JiraLink sut = new JiraLink();
            Assert.That(() => sut.Equals(null), Is.False);
        }
    }
}