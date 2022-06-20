using NUnit.Framework;

using JiraExport;
using AutoFixture.AutoNSubstitute;
using AutoFixture;
using Migration.WIContract;
using Common.Config;
using System.Collections.Generic;
using Migration.Common;
using Migration.Common.Config;
using Newtonsoft.Json.Linq;
using NSubstitute;

namespace Migration.Jira_Export.Tests
{
    [TestFixture]
    public class JiraMapperTests
    {
        // use auto fixiture to help mock and instantiate with dummy data with nsubsitute. 
        private Fixture _fixture;

        [SetUp]
        public void Setup()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoNSubstituteCustomization() { });
        }

        //Reorder public/private methods
 

        [Test]
        public void When_calling_map_Then_the_expected_result_is_returned()
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

            JiraSettings settings = new JiraSettings("userID", "pass", "url", "project");
            settings.EpicLinkField = "EpicLinkField";
            settings.SprintField = "SprintField";

            provider.GetSettings().ReturnsForAnyArgs(settings);

            JiraItem jiraItem = JiraItem.CreateFromRest(issueKey, provider);

            var provider2 = _fixture.Freeze<IJiraProvider>();
            provider2.GetSettings().ReturnsForAnyArgs(settings);

            ConfigJson cjson = new ConfigJson();
            TypeMap t = new TypeMap();
            t.Types = new List<Type>();
            FieldMap f = new FieldMap();
            f.Fields = new List<Field>();
            cjson.TypeMap = t;
            Type type = new Type();
            type.Source = "Story";
            type.Target = "User Story";
            t.Types.Add(type);
            cjson.FieldMap = f;

            WiItem expectedWiItem = new WiItem();
            expectedWiItem.Type = "User Story";
            expectedWiItem.OriginId = issueKey;

            JiraMapper sut = new JiraMapper(provider2, cjson);

            WiItem expected = expectedWiItem;
            WiItem actual = sut.Map(jiraItem);

            Assert.AreEqual(expected.OriginId, actual.OriginId);
            Assert.AreEqual(expected.Type, actual.Type);
        }

        [Test]
        public void When_calling_maplinks_Then_the_expected_result_is_returned()
        {
            JiraItem jiraItem = _fixture.Create<JiraItem>();
            JiraRevision jiraRevision = new JiraRevision(jiraItem);

            JiraMapper sut = _fixture.Create<JiraMapper>();

            List<WiLink> expected = new List<WiLink>();
            List<WiLink> actual = sut.MapLinks(jiraRevision);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void When_calling_mapattachments_Then_the_expected_result_is_returned()
        {
            JiraItem jiraItem = _fixture.Create<JiraItem>();
            JiraRevision jiraRevision = new JiraRevision(jiraItem);

            JiraMapper sut = _fixture.Create<JiraMapper>();

            List<WiAttachment> expected = new List<WiAttachment>();
            List<WiAttachment> actual = sut.MapAttachments(jiraRevision);

            Assert.AreEqual(expected, actual);
        }

        //MapFields
        [Test]
        public void When_calling_mapfields_Then_the_expected_result_is_returned()
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

            JiraSettings settings = new JiraSettings("userID", "pass", "url", "project");
            settings.EpicLinkField = "EpicLinkField";
            settings.SprintField = "SprintField";

            provider.GetSettings().ReturnsForAnyArgs(settings);

            JiraItem jiraItem = JiraItem.CreateFromRest(issueKey, provider);

            var provider2 = _fixture.Freeze<IJiraProvider>();
            provider2.GetSettings().ReturnsForAnyArgs(settings);

            ConfigJson cjson = new ConfigJson();
            TypeMap t = new TypeMap();
            t.Types = new List<Type>();
            FieldMap f = new FieldMap();
            f.Fields = new List<Field>();
            cjson.TypeMap = t;
            Type type = new Type();
            type.Source = "Story";
            type.Target = "User Story";
            t.Types.Add(type);
            cjson.FieldMap = f;

            JiraRevision jiraRevision = new JiraRevision(jiraItem);

            List<WiField> expectedWiFieldList = new List<WiField>();

            JiraMapper sut = new JiraMapper(provider2, cjson);

            List<WiField> expected = expectedWiFieldList;
            List<WiField> actual = sut.MapFields(jiraRevision);

            Assert.AreEqual(expected, actual);
        }

        //InitializeFieldMappings
        //[Test]
        //public void When_calling_initializefieldmappings_Then_the_expected_result_is_returned()
        //{
        //    JiraMapper sut = _fixture.Create<JiraMapper>();

        //    var expected = new Dictionary<string, FieldMapping<JiraRevision>>();
        //    var actual = sut.InitializeFieldMappings();

        //    Assert.AreEqual(expected, actual);
        //}
    }
}