using AutoFixture;
using AutoFixture.AutoNSubstitute;
using JiraExport;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Diagnostics.CodeAnalysis;

namespace Migration.Jira_Export.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class JiraChangeItemTests
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
        public void When_creating_a_jirachangeitem_object_Then_an_object_is_created()
        {
            JObject jobj = _fixture.Create<JObject>();

            JiraChangeItem sut = new JiraChangeItem(jobj);

            Assert.That(sut, Is.Not.Null);
        }
    }
}
