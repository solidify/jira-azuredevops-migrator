using AutoFixture;
using AutoFixture.AutoNSubstitute;
using Common.Config;
using JiraExport;
using Migration.Common;
using Migration.Common.Config;
using Migration.WIContract;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Type = Migration.Common.Config.Type;

namespace Migration.Jira_Export.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class JiraMapperTests
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
        public void When_calling_map_Then_the_expected_result_is_returned()
        {
            JiraItem jiraItem = CreateJiraItem();

            WiItem expectedWiItem = new WiItem
            {
                Type = "User Story",
                OriginId = "issue_key"
            };

            JiraMapper sut = CreateJiraMapper();

            WiItem expected = expectedWiItem;
            WiItem actual = sut.Map(jiraItem);

            Assert.Multiple(() =>
            {
                Assert.AreEqual(expected.OriginId, actual.OriginId);
                Assert.AreEqual(expected.Type, actual.Type);
            });
        }

        [Test]
        public void When_calling_map_with_null_arguments_Then_and_exception_is_thrown()
        {
            JiraItem jiraItem = CreateJiraItem();
            JiraMapper sut = CreateJiraMapper();

            Assert.Throws<System.ArgumentNullException>(() => { sut.Map(null); });
        }

        [Test]
        public void When_calling_map_on_an_issue_with_an_epic_link_and_a_parent_Then_two_parent_links_are_mapped()
        {
            //Arrange
            var provider = _fixture.Freeze<IJiraProvider>();
            long issueId = _fixture.Create<long>();
            string issueKey = _fixture.Create<string>();
            string epicId = _fixture.Create<long>().ToString();
            string epicKey = "EpicKey";
            string parentId = _fixture.Create<long>().ToString();
            string parentKey = "ParentKey";

            var fields = JObject.Parse(@"{
                'issuetype': {'name': 'Story'},
                'EpicLinkField': 'EpicKey'
            }");
            var renderedFields = JObject.Parse("{ 'custom_field_name': 'SomeValue', 'description': 'RenderedDescription' }");

            var changelog = new List<JObject>() {
                new HistoryItem()
                {
                    Field = "Epic Link",
                    FieldType = "custom",
                    To = epicId,
                    ToString = epicKey
                }.ToJObject(),
                new HistoryItem()
                {
                    Id = 1,
                    Field = "Parent",
                    FieldType = "jira",
                    To = parentId,
                    ToString = parentKey
                }.ToJObject()
            };

            JObject remoteIssue = new JObject
            {
                { "id", issueId },
                { "key", issueKey },
                { "fields", fields },
                { "renderedFields", renderedFields }
            };

            provider.DownloadIssue(default).ReturnsForAnyArgs(remoteIssue);
            provider.DownloadChangelog(default).ReturnsForAnyArgs(changelog);
            var jiraSettings = CreateJiraSettings();
            provider.GetSettings().ReturnsForAnyArgs(jiraSettings);
            var jiraItem = JiraItem.CreateFromRest(issueKey, provider);
            JiraMapper sut = CreateJiraMapper();

            //Act
            WiItem actual = sut.Map(jiraItem);

            //Assert
            Assert.Multiple(() =>
            {
                Assert.AreEqual(3, actual.Revisions.Count());
                Assert.AreEqual(0, actual.Revisions[0].Links.Count());
                Assert.AreEqual(1, actual.Revisions[1].Links.Count());
                Assert.AreEqual(epicKey, actual.Revisions[1].Links[0].TargetOriginId);
                Assert.AreEqual(1, actual.Revisions[2].Links.Count());
                Assert.AreEqual(parentKey, actual.Revisions[2].Links[0].TargetOriginId);
            });
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
        public void When_calling_maplinks_with_null_arguments_Then_and_exception_is_thrown()
        {
            JiraItem jiraItem = CreateJiraItem();
            JiraMapper sut = CreateJiraMapper();

            Assert.Throws<System.ArgumentNullException>(() => { sut.MapLinks(null); });
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

        [Test]
        public void When_calling_mapattachments_with_null_arguments_Then_and_exception_is_thrown()
        {
            JiraItem jiraItem = CreateJiraItem();
            JiraMapper sut = CreateJiraMapper();

            Assert.Throws<System.ArgumentNullException>(() => { sut.MapAttachments(null); });
        }

        [Test]
        public void When_calling_mapfields_with_null_arguments_Then_and_exception_is_thrown()
        {
            JiraItem jiraItem = CreateJiraItem();
            JiraMapper sut = CreateJiraMapper();

            Assert.Throws<System.ArgumentNullException>(() => { sut.MapFields(null); });
        }

        [Test]
        public void When_calling_mapfields_Then_the_expected_result_is_returned()
        {
            JiraItem jiraItem = CreateJiraItem();
            JiraRevision jiraRevision = new JiraRevision(jiraItem);
            List<WiField> expectedWiFieldList = new List<WiField>();

            JiraMapper sut = CreateJiraMapper();

            List<WiField> expected = expectedWiFieldList;
            List<WiField> actual = sut.MapFields(jiraRevision);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void When_calling_truncatefields_with_too_long_title_Then_a_truncated_title_returned()
        {
            string sourceTitle =
                "test task with max name length - 0123456789012345678901234567890123456789012345"
                + "678901234567890123456789012345678901234567890123456789012345678901234567890123"
                + "456789012345678901234567890123456789012345678901234567890123456789012345678901"
                + "23456789012345678901234567890";
            string expected =
                "test task with max name length - 0123456789012345678901234567890123456789012345"
                + "678901234567890123456789012345678901234567890123456789012345678901234567890123"
                + "456789012345678901234567890123456789012345678901234567890123456789012345678901"
                + "23456789012345678...";

            JiraMapper sut = CreateJiraMapper();
            string actual = (string)sut.TruncateField(sourceTitle, WiFieldReference.Title);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void When_calling_initializefieldmappings_Then_the_expected_result_is_returned()
        {
            var expectedDictionary = new Dictionary<string, FieldMapping<JiraRevision>>();
            var fieldmap = new FieldMapping<JiraRevision>();
            expectedDictionary.Add("User Story", fieldmap);

            JiraMapper sut = CreateJiraMapper();

            var exportIssuesSummary = new ExportIssuesSummary();

            var expected = expectedDictionary;
            var actual = sut.InitializeFieldMappings(exportIssuesSummary);

            Assert.AreEqual(expected, actual);
        }

        private JiraSettings CreateJiraSettings()
        {
            JiraSettings settings = new JiraSettings("userID", "pass", "token", "url", "project")
            {
                EpicLinkField = "Epic Link",
                SprintField = "SprintField"
            };

            return settings;
        }

        private JiraMapper CreateJiraMapper()
        {
            var provider = _fixture.Freeze<IJiraProvider>();
            provider.GetSettings().ReturnsForAnyArgs(CreateJiraSettings());

            ConfigJson cjson = new ConfigJson();

            FieldMap f = new FieldMap
            {
                Fields = new List<Field>()
            };
            cjson.FieldMap = f;

            TypeMap t = new TypeMap
            {
                Types = new List<Type>()
            };
            Type type = new Type
            {
                Source = "Story",
                Target = "User Story"
            };
            t.Types.Add(type);
            cjson.TypeMap = t;

            LinkMap linkMap = new LinkMap
            {
                Links = new List<Link>()
            };
            var epicLinkMap = new Link() { Source = "Epic", Target = "System.LinkTypes.Hierarchy-Reverse" };
            var parentLinkMap = new Link() { Source = "Parent", Target = "System.LinkTypes.Hierarchy-Reverse" };
            linkMap.Links.AddRange(new Link[] { epicLinkMap, parentLinkMap });
            cjson.LinkMap = linkMap;

            RepositoryMap repositoryMap = new RepositoryMap
            {
                Repositories = new List<Repository>()
            };
            Repository repository = new Repository
            {
                Source = "Sample Repository",
                Target = "Destination Repository"
            };
            repositoryMap.Repositories.Add(repository);
            cjson.RepositoryMap = repositoryMap;

            var exportIssuesSummary = new ExportIssuesSummary();

            JiraMapper sut = new JiraMapper(provider, cjson, exportIssuesSummary);

            return sut;
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
    }
}