using NUnit.Framework;

using JiraExport;
using AutoFixture.AutoNSubstitute;
using AutoFixture;

namespace Migration.Jira_Export.Tests
{
    [TestFixture]
    public class JiraAttachmentTests
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
            JiraAttachment sut = new JiraAttachment();

            sut.Id = "id";
            sut.Filename = "name";

            string expectedToString = $"{sut.Id}/{sut.Filename}";

            Assert.That(() => sut.ToString(), Is.EqualTo(expectedToString));
        }

        [Test]
        public void When_calling_equals_with_two_equal_jira_attachments_Then_true_is_returned()
        {
            JiraAttachment sut1 = new JiraAttachment();
            JiraAttachment sut2 = new JiraAttachment();
            
            string idString = "id";

            sut1.Id = idString;
            sut2.Id = idString;

            Assert.That(() => sut1.Equals(sut2), Is.True);
        }

        [Test]
        public void When_calling_equals_with_null_argumentss_Then_false_is_returned()
        {
            JiraAttachment sut = new JiraAttachment();
            Assert.That(() => sut.Equals(null), Is.False);
        }
    }
}