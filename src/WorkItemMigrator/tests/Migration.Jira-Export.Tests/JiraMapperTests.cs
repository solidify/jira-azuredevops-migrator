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
using System.Linq;
using System.Diagnostics.CodeAnalysis;

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
            JiraItem jiraItem = createJiraItem();

            WiItem expectedWiItem = new WiItem();
            expectedWiItem.Type = "User Story";
            expectedWiItem.OriginId = "issue_key";

            JiraMapper sut = createJiraMapper();

            WiItem expected = expectedWiItem;
            WiItem actual = sut.Map(jiraItem);

            Assert.Multiple(() =>
            {
                Assert.AreEqual(expected.OriginId, actual.OriginId);
                Assert.AreEqual(expected.Type, actual.Type);
                Assert.AreEqual(1, actual.Revisions.Count);
            });
        }

        [Test]
        public void When_calling_map_but_no_fields_are_mapped_Then_no_revision_is_returned()
        {
            JiraItem jiraItem = createJiraItem();

            WiItem expectedWiItem = new WiItem();
            expectedWiItem.Type = "User Story";
            expectedWiItem.OriginId = "issue_key";

            ConfigJson config = new ConfigJson();
            config.ExcludeEmptyRevisions = true;
            JiraMapper sut = createJiraMapper(null, config);

            WiItem expected = expectedWiItem;
            WiItem actual = sut.Map(jiraItem);

            Assert.Multiple(() =>
            {
                Assert.AreEqual(expected.OriginId, actual.OriginId);
                Assert.AreEqual(expected.Type, actual.Type);
                Assert.AreEqual(0, actual.Revisions.Count);
            });
        }

        [Test]
        public void When_calling_map_Then_sequential_revisions_should_be_returned()
        {
            //Arrange
            var extraFields = new Dictionary<string, string>();
            for (int i = 0; i < 10; i++)
            {
                extraFields.Add(_fixture.Create<string>(), _fixture.Create<string>());
            }
            JiraItem jiraItem = createJiraItem(extraFields);

            //Add a revision updating each extra field with a new value
            for (int i = 0; i < 10; i++)
            {
                AddUpdateFieldRevisionToJiraItem(jiraItem, extraFields.ElementAt(i).Key, _fixture.Create<string>());
            }

            WiItem expectedWiItem = new WiItem();
            expectedWiItem.Type = "User Story";
            expectedWiItem.OriginId = "issue_key";

            ConfigJson config = new ConfigJson();
            config.ExcludeEmptyRevisions = true;

            //Only map extraFields 3 and 7
            FieldMap fieldMap = new FieldMap();
            fieldMap.Fields = new List<Field>();
            foreach (int i in new int[] { 3, 7 })
            {
                fieldMap.Fields.Add(new Field()
                {
                    Source = extraFields.ElementAt(i).Key,
                    Target = extraFields.ElementAt(i).Value
                }
                );
            };

            JiraMapper sut = createJiraMapper(fieldMap, config);

            WiItem expected = expectedWiItem;
            WiItem actual = sut.Map(jiraItem);

            Assert.Multiple(() =>
            {
                Assert.AreEqual(3, actual.Revisions.Count);
                //Index starts at 0
                Assert.AreEqual(0, actual.Revisions[0].Index);
                //Revision Indexes are sequential [0,1,2,3...]
                Assert.IsTrue(Enumerable.Range(0, actual.Revisions.Count)
                    .All(i => actual.Revisions[i].Index == actual.Revisions[0].Index + i));
            });
        }

        [Test]
        public void When_calling_map_and_fields_are_mapped_Then_the_expected_result_is_returned()
        {
            JiraItem jiraItem = createJiraItem();

            WiItem expectedWiItem = new WiItem();
            expectedWiItem.Type = "User Story";
            expectedWiItem.OriginId = "issue_key";

            Field field = new Field();
            field.Source = "description";
            field.Target = "System.Description";
            FieldMap fieldMap = new FieldMap();
            fieldMap.Fields = new List<Field>() { field };
            ConfigJson config = new ConfigJson();
            config.ExcludeEmptyRevisions = true;
            JiraMapper sut = createJiraMapper(fieldMap, config);


            WiItem expected = expectedWiItem;
            WiItem actual = sut.Map(jiraItem);

            Assert.Multiple(() =>
            {
                Assert.AreEqual(expected.OriginId, actual.OriginId);
                Assert.AreEqual(expected.Type, actual.Type);
                Assert.AreEqual(1, actual.Revisions.Count);
            });
        }

        [Test]
        public void When_calling_map_with_null_arguments_Then_and_exception_is_thrown()
        {
            JiraItem jiraItem = createJiraItem();
            JiraMapper sut = createJiraMapper();

            Assert.Throws<System.ArgumentNullException>(() => { sut.Map(null); });
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
            JiraItem jiraItem = createJiraItem();
            JiraMapper sut = createJiraMapper();

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
            JiraItem jiraItem = createJiraItem();
            JiraMapper sut = createJiraMapper();

            Assert.Throws<System.ArgumentNullException>(() => { sut.MapAttachments(null); });
        }

        [Test]
        public void When_calling_mapfields_Then_the_expected_result_is_returned()
        {
            JiraItem jiraItem = createJiraItem();
            JiraRevision jiraRevision = new JiraRevision(jiraItem);
            List<WiField> expectedWiFieldList = new List<WiField>();

            JiraMapper sut = createJiraMapper();

            List<WiField> expected = expectedWiFieldList;
            List<WiField> actual = sut.MapFields(jiraRevision);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void When_calling_mapfields_with_null_arguments_Then_and_exception_is_thrown()
        {
            JiraItem jiraItem = createJiraItem();
            JiraMapper sut = createJiraMapper();

            Assert.Throws<System.ArgumentNullException>(() => { sut.MapFields(null); });
        }

        [Test]
        public void When_calling_initializefieldmappings_Then_the_expected_result_is_returned()
        {
            var expectedDictionary = new Dictionary<string, FieldMapping<JiraRevision>>();
            var fieldmap = new FieldMapping<JiraRevision>();
            expectedDictionary.Add("User Story", fieldmap);

            JiraMapper sut = createJiraMapper();

            var expected = expectedDictionary;
            var actual = sut.InitializeFieldMappings();

            Assert.AreEqual(expected, actual);
        }

        private JiraSettings createJiraSettings()
        {
            JiraSettings settings = new JiraSettings("userID", "pass", "url", "project");
            settings.EpicLinkField = "EpicLinkField";
            settings.SprintField = "SprintField";

            return settings;
        }

        private JiraMapper createJiraMapper(FieldMap fieldMap = null, ConfigJson config = null)
        {
            var provider = _fixture.Freeze<IJiraProvider>();
            provider.GetSettings().ReturnsForAnyArgs(createJiraSettings());

            ConfigJson cjson = config ?? new ConfigJson();
            TypeMap t = new TypeMap();
            t.Types = new List<Type>();
            FieldMap f = new FieldMap();
            f.Fields = new List<Field>();
            cjson.TypeMap = t;
            Type type = new Type();
            type.Source = "Story";
            type.Target = "User Story";
            t.Types.Add(type);
            cjson.FieldMap = fieldMap ?? f;

            JiraMapper sut = new JiraMapper(provider, cjson);

            return sut;
        }

        private JiraItem createJiraItem(Dictionary<string, string> extraFields = null)
        {
            var provider = _fixture.Freeze<IJiraProvider>();

            var fields = JObject.Parse($"{{ 'issuetype': {{'name': 'Story'}}, " +
                $"'custom_field_name': 'SomeValue', " +
                $"'description': 'RenderedDescription'}}");

            if (extraFields != null)
            {
                foreach (var field in extraFields)
                {
                    fields[field.Key] = field.Value;
                }
            }

            var renderedFields = JObject.Parse($"{{ 'custom_field_name': 'SomeValue', " +
                $"'description': 'RenderedDescription' }}");
            string issueKey = "issue_key";

            JObject remoteIssue = new JObject
            {
                { "fields", fields },
                { "renderedFields", renderedFields },
                { "key", issueKey }
            };

            provider.DownloadIssue(default).ReturnsForAnyArgs(remoteIssue);
            provider.GetSettings().ReturnsForAnyArgs(createJiraSettings());

            JiraItem jiraItem = JiraItem.CreateFromRest(issueKey, provider);

            return jiraItem;
        }

        private void AddUpdateFieldRevisionToJiraItem(JiraItem parentItem, string fieldName, string fieldValue)
        {
            JiraRevision revision = new JiraRevision(parentItem);
            revision.Index = parentItem.Revisions.Count;
            revision.Fields = new Dictionary<string, object>();
            revision.Fields[fieldName] = fieldValue;
            parentItem.Revisions.Add(revision);
        }
    }
}