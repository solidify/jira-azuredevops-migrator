using NUnit.Framework;

using JiraExport;
using AutoFixture.AutoNSubstitute;
using AutoFixture;
using Newtonsoft.Json.Linq;
using NSubstitute;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;

namespace Migration.Jira_Export.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class JiraItemTests
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
        public void When_a_parent_link_is_added_later_Then_it_should_not_be_in_the_initial_revision()
        {
            //Arrange
            var provider = _fixture.Freeze<IJiraProvider>();
            long issueId = _fixture.Create<long>();
            string issueKey = _fixture.Create<string>();
            string parentId = _fixture.Create<long>().ToString();
            string parentKey = "ISSUE-xx";

            var fields = JObject.Parse($@"{{
                'issuetype': {{ 'name': 'Story' }},
                'parent': {{ 'id': '{parentId}', 'key': '{parentKey}' }}
            }}");
            var renderedFields = JObject.Parse("{ 'custom_field_name': 'SomeValue', 'description': 'RenderedDescription' }");

            var changelog = new List<JObject>() { 
                new HistoryItem() 
                {
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
            var jiraSettings = createJiraSettings();
            provider.GetSettings().ReturnsForAnyArgs(jiraSettings);

            //Act
            var jiraItem = JiraItem.CreateFromRest(issueKey, provider);

            //Assert
            Assert.IsFalse(jiraItem.Revisions[0].Fields.ContainsKey("parent"));
            Assert.IsTrue(jiraItem.Revisions[1].Fields.ContainsKey("parent"));
        }

        [Test]
        public void When_a_parent_link_is_changed_later_Then_it_should_not_be_in_the_initial_revision()
        {
            //Arrange
            var provider = _fixture.Freeze<IJiraProvider>();
            long issueId = _fixture.Create<long>();
            string issueKey = _fixture.Create<string>();
            string previousParentId = _fixture.Create<long>().ToString();
            string previousParentKey = "ISSUE-xx";
            string currentParentId = _fixture.Create<long>().ToString();
            string currentParentKey = "ISSUE-yy";

            var fields = JObject.Parse($@"{{
                'issuetype': {{ 'name': 'Story' }},
                'parent': {{ 'id': '{currentParentId}', 'key': '{currentParentKey}' }}
            }}");
            var renderedFields = JObject.Parse("{ 'custom_field_name': 'SomeValue', 'description': 'RenderedDescription' }");

            var changelog = new List<JObject>() { 
                new HistoryItem() 
                {
                    Field = "Parent",
                    FieldType = "jira",
                    From = previousParentId,
                    FromString = previousParentKey,
                    To = currentParentId,
                    ToString = currentParentKey
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
            var jiraSettings = createJiraSettings();
            provider.GetSettings().ReturnsForAnyArgs(jiraSettings);

            //Act
            var jiraItem = JiraItem.CreateFromRest(issueKey, provider);

            //Assert
            Assert.AreEqual(previousParentKey, jiraItem.Revisions[0].Fields["parent"]);
            Assert.AreEqual(currentParentKey, jiraItem.Revisions[1].Fields["parent"]);
        }

        [Test]
        public void When_a_parent_link_is_added_and_changed_later_Then_it_should_not_be_in_the_initial_revision()
        {
            //Arrange
            var provider = _fixture.Freeze<IJiraProvider>();
            long issueId = _fixture.Create<long>();
            string issueKey = _fixture.Create<string>();
            string previousParentId = _fixture.Create<long>().ToString();
            string previousParentKey = "PreviousParentKey";
            string currentParentId = _fixture.Create<long>().ToString();
            string currentParentKey = "CurrentParentKey";

            var fields = JObject.Parse($@"{{
                'issuetype': {{ 'name': 'Story' }}
            }}");
            var renderedFields = JObject.Parse("{ 'custom_field_name': 'SomeValue', 'description': 'RenderedDescription' }");

            var changelog = new List<JObject>() {
                new HistoryItem() 
                {
                    Id = 0,
                    Field = "Parent",
                    FieldType = "jira",
                    To = previousParentId,
                    ToString = previousParentKey
                }.ToJObject(), 
                new HistoryItem() 
                {
                    Id = 1,
                    Field = "Parent",
                    FieldType = "jira",
                    From = previousParentId,
                    FromString = previousParentKey,
                    To = currentParentId,
                    ToString = currentParentKey
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
            var jiraSettings = createJiraSettings();
            provider.GetSettings().ReturnsForAnyArgs(jiraSettings);

            //Act
            var jiraItem = JiraItem.CreateFromRest(issueKey, provider);

            //Assert
            Assert.IsFalse(jiraItem.Revisions[0].Fields.ContainsKey("parent"));
            Assert.AreEqual(previousParentKey, jiraItem.Revisions[1].Fields["parent"]);
            Assert.AreEqual(currentParentKey, jiraItem.Revisions[2].Fields["parent"]);
        }

        [Test]
        public void When_a_parent_link_was_removed_Then_the_result_should_be_succesful()
        {
            //Arrange
            var provider = _fixture.Freeze<IJiraProvider>();
            long issueId = _fixture.Create<long>();
            string issueKey = _fixture.Create<string>();
            string previousParentId = _fixture.Create<long>().ToString();
            string previousParentKey = "ISSUE-xx";

            var fields = JObject.Parse($@"{{
                'issuetype': {{ 'name': 'Story' }}
            }}");
            var renderedFields = JObject.Parse("{ 'custom_field_name': 'SomeValue', 'description': 'RenderedDescription' }");

            var changelog = new List<JObject>() { 
                new HistoryItem() 
                {
                    Field = "Parent",
                    FieldType = "jira",
                    From = previousParentId,
                    FromString = previousParentKey
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
            var jiraSettings = createJiraSettings();
            provider.GetSettings().ReturnsForAnyArgs(jiraSettings);

            //Act
            var jiraItem = JiraItem.CreateFromRest(issueKey, provider);

            //Assert
            Assert.AreEqual(2, jiraItem.Revisions.Count);
        }

        [Test]
        public void When_an_epic_link_is_added_later_Then_it_should_not_be_in_the_initial_revision()
        {
            //Arrange
            var provider = _fixture.Freeze<IJiraProvider>();
            long issueId = _fixture.Create<long>();
            string issueKey = _fixture.Create<string>();
            string epicId = _fixture.Create<long>().ToString();
            string epicKey = "EpicKey";

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
            var jiraSettings = createJiraSettings();
            provider.GetSettings().ReturnsForAnyArgs(jiraSettings);

            //Act
            var jiraItem = JiraItem.CreateFromRest(issueKey, provider);

            //Assert
            Assert.IsFalse(jiraItem.Revisions[0].Fields.ContainsKey(jiraSettings.EpicLinkField));
            Assert.IsTrue(jiraItem.Revisions[1].Fields.ContainsKey(jiraSettings.EpicLinkField));
        }

        [Test]
        public void When_an_epic_link_is_changed_Then_it_should_have_the_previous_value_in_the_initial_revision()
        {
            //Arrange
            var provider = _fixture.Freeze<IJiraProvider>();
            long issueId = _fixture.Create<long>();
            string issueKey = _fixture.Create<string>();
            string currentEpicId = _fixture.Create<long>().ToString();
            string currentEpicKey = "EpicKey";
            string previousEpicId = _fixture.Create<long>().ToString();
            string previousEpicKey = "PreviousEpicKey";

            var fields = JObject.Parse(@"{'issuetype': {'name': 'Story'},'EpicLinkField': 'EpicKey'}");
            var renderedFields = JObject.Parse("{ 'custom_field_name': 'SomeValue', 'description': 'RenderedDescription' }");

            var changelog = new List<JObject>() { 
                new HistoryItem() 
                {
                    Field = "Epic Link",
                    FieldType = "custom",
                    From = previousEpicId,
                    FromString = previousEpicKey,
                    To = currentEpicId,
                    ToString = currentEpicKey
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
            var jiraSettings = createJiraSettings();
            provider.GetSettings().ReturnsForAnyArgs(jiraSettings);

            //Act
            var jiraItem = JiraItem.CreateFromRest(issueKey, provider);

            //Assert
            Assert.AreEqual(previousEpicKey,jiraItem.Revisions[0].Fields[jiraSettings.EpicLinkField]);
            Assert.AreEqual(currentEpicKey, jiraItem.Revisions[1].Fields[jiraSettings.EpicLinkField]);
        }

        [Test]
        public void When_an_epic_link_is__added_and_changed_later_Then_it_should_not_be_in_the_initial_revision()
        {
            //Arrange
            var provider = _fixture.Freeze<IJiraProvider>();
            long issueId = _fixture.Create<long>();
            string issueKey = _fixture.Create<string>();
            string currentEpicId = _fixture.Create<long>().ToString();
            string currentEpicKey = "EpicKey";
            string previousEpicId = _fixture.Create<long>().ToString();
            string previousEpicKey = "PreviousEpicKey";

            var fields = JObject.Parse(@"{'issuetype': {'name': 'Story'},'EpicLinkField': null}");
            var renderedFields = JObject.Parse("{ 'custom_field_name': 'SomeValue', 'description': 'RenderedDescription' }");

            var changelog = new List<JObject>() { 
                new HistoryItem() 
                {
                    Field = "Epic Link",
                    FieldType = "custom",
                    To = previousEpicId,
                    ToString = previousEpicKey
                }.ToJObject(), 
                new HistoryItem()
                {
                    Id = 1,
                    Field = "Epic Link",
                    FieldType = "custom",
                    From = previousEpicId,
                    FromString = previousEpicKey,
                    To = currentEpicId,
                    ToString = currentEpicKey
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
            var jiraSettings = createJiraSettings();
            provider.GetSettings().ReturnsForAnyArgs(jiraSettings);

            //Act
            var jiraItem = JiraItem.CreateFromRest(issueKey, provider);

            //Assert
            Assert.IsFalse(jiraItem.Revisions[0].Fields.ContainsKey(jiraSettings.EpicLinkField));
            Assert.AreEqual(previousEpicKey, jiraItem.Revisions[1].Fields[jiraSettings.EpicLinkField]);
            Assert.AreEqual(currentEpicKey, jiraItem.Revisions[2].Fields[jiraSettings.EpicLinkField]);
        }

        [Test]
        public void When_an_epic_link_was_removed_Then_the_result_should_be_successful()
        {
            //Arrange
            var provider = _fixture.Freeze<IJiraProvider>();
            long issueId = _fixture.Create<long>();
            string issueKey = _fixture.Create<string>();
            string previousEpicId = _fixture.Create<long>().ToString();
            string previousEpicKey = "PreviousEpicKey";

            var fields = JObject.Parse(@"{'issuetype': {'name': 'Story'},'EpicLinkField': 'EpicKey'}");
            var renderedFields = JObject.Parse("{ 'custom_field_name': 'SomeValue', 'description': 'RenderedDescription' }");

            var changelog = new List<JObject>() { 
                new HistoryItem() 
                {
                    Field = "Epic Link",
                    FieldType = "custom",
                    From = previousEpicId,
                    FromString = previousEpicKey
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
            var jiraSettings = createJiraSettings();
            provider.GetSettings().ReturnsForAnyArgs(jiraSettings);

            //Act
            var jiraItem = JiraItem.CreateFromRest(issueKey, provider);

            //Assert
            Assert.AreEqual(2, jiraItem.Revisions.Count);
        }

        private JiraSettings createJiraSettings()
        {
            JiraSettings settings = new JiraSettings("userID", "pass", "url", "project");
            settings.EpicLinkField = "EpicLinkField";
            settings.SprintField = "SprintField";

            return settings;
        }
    }
}