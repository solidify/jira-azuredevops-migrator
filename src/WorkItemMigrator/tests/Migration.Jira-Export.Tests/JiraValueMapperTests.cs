using NUnit.Framework;
using NSubstitute;
using AutoFixture;
using Common.Config;
using JiraExport;
using Migration.Common.Config;
using System.Collections.Generic;
using System;
using Newtonsoft.Json.Linq;
using Type = Migration.Common.Config.Type;
using AutoFixture.AutoNSubstitute;

namespace Migration.Jira_Export.Tests
{

    [TestFixture]
    public class JiraValueMapperTests
    {
        // use auto fixture to help mock and instantiate with dummy data with nsubsitute. 
        private Fixture _fixture;        
        private ConfigJson _config;
        private JiraItem _item;
        private IJiraProvider _provider;

        [SetUp]
        public void SetupValueMapperTests()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoNSubstituteCustomization() { });
            _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

            _config = new ConfigJson
            {
                TypeMap = new TypeMap
                {
                    Types = new List<Type>
                    {
                        new Type { Source = "Bug", Target = "Defect" },
                        new Type { Source = "Task", Target = "Work Item" }
                    }
                },
                FieldMap = new FieldMap
                {
                    Fields = new List<Field>
                    {
                        new Field
                        {
                            Source = "Priority",
                            Target = "Severity",
                            For = "All",
                            Mapping = new Mapping
                            {
                                Values = new List<Value>
                                {
                                    new Value { Source = "High", Target = "Critical" },
                                    new Value { Source = "Medium", Target = "Major" },
                                    new Value { Source = "Low", Target = "Minor" }
                                }
                            }
                        },
                        new Field
                        {
                            Source = "Status",
                            Target = "State",
                            For = "Defect",
                            Mapping = new Mapping
                            {
                                Values = new List<Value>
                                {
                                    new Value { Source = "Open", Target = "Active" },
                                    new Value { Source = "In Progress", Target = "In Development" },
                                    new Value { Source = "Resolved", Target = "Fixed" },
                                    new Value { Source = "Closed", Target = "Closed" }
                                }
                            }
                        },
                        new Field
                        {
                            Source = "Empty",
                            Target = "Mapping",
                            Mapping = null
                        }
                    }
                }
            };

            _provider = CreateJiraProvider();
            string issueKey = "issue_key";
            _item = JiraItem.CreateFromRest(issueKey, _provider);
        }

        [Test]
        public void MapValue_WithNullRevision_ThrowsArgumentNullException()
        {
            // Arrange
            JiraRevision revision = null;
            string itemSource = "Priority";
            string itemTarget = "Severity";

            var exportIssuesSummary = new ExportIssuesSummary();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => FieldMapperUtils.MapValue(revision, itemSource, itemTarget, _config, exportIssuesSummary));
        }

        [Test]
        public void MapValue_WithNullConfig_ThrowsArgumentNullException()
        {
            // Arrange
            ConfigJson config = null;
            string itemSource = "Priority";
            string itemTarget = "Severity";
            var revision = new JiraRevision(_item)
            {
                Fields = new Dictionary<string, object>
                {
                    { "Priority", "High" },
                    { "Status", "Open" }
                }
            };

            var exportIssuesSummary = new ExportIssuesSummary();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => FieldMapperUtils.MapValue(revision, itemSource, itemTarget, config, exportIssuesSummary));
        }

        [Test]
        public void MapValue_WithNonExistingField_ReturnsFalseAndNull()
        {
            // Arrange
            string itemSource = "NonExistingField";
            string itemTarget = "Severity";
            var revision = new JiraRevision(_item)
            {
                Fields = new Dictionary<string, object>
                {
                    { "Priority", "High" },
                    { "Status", "Open" }
                }
            };

            var exportIssuesSummary = new ExportIssuesSummary();

            // Act
            var result = FieldMapperUtils.MapValue(revision, itemSource, itemTarget, _config, exportIssuesSummary);

            // Assert
            Assert.IsFalse(result.Item1);
            Assert.IsNull(result.Item2);
        }

        [Test]
        public void MapValue_WithExistingFieldAndMapping_ReturnsTrueAndMappedValue()
        {
            // Arrange
            string itemSource = "Priority";
            string itemTarget = "Severity";
            var revision = new JiraRevision(_item)
            {
                Fields = new Dictionary<string, object>
                {
                    { "Priority", "High" },
                    { "Status", "Open" }
                }
            };

            var exportIssuesSummary = new ExportIssuesSummary();

            // Act
            var result = FieldMapperUtils.MapValue(revision, itemSource, itemTarget, _config, exportIssuesSummary);

            // Assert
            Assert.IsTrue(result.Item1);
            Assert.AreEqual("Critical", result.Item2);
        }

        [Test]
        public void MapValue_WithMatchesNotForButTargetDoesNotMatch_ReturnsFalseAndNull()
        {
            // Arrange
            string itemSource = "Status";
            string itemTarget = "target";
            var revision = new JiraRevision(_item)  // type is Bug by default;
            {
                Fields = new Dictionary<string, object>
                {
                    { "Status", "Open" },
                    { "SomethingElse", "SomethingElse" }
                }
            };
            var typeMap = new TypeMap
            {
                Types = new List<Type>
                {
                    new Type { Source = "Bug", Target = "Bug" },
                    new Type { Source = "Task", Target = "Work Item" }
                }
            };
            var fieldConfig = new Field
            {
                Source = "Status",
                Target = "XXXNotStateXXX",
                NotFor = "Defect",
                Mapping = new Mapping
                {
                    Values = new List<Value>
                    {
                        new Value { Source = "Open", Target = "Active" },
                        new Value { Source = "In Progress", Target = "In Development" },
                        new Value { Source = "Resolved", Target = "Fixed" },
                        new Value { Source = "Closed", Target = "Closed" }
                    }
                }
            };

            var config = new ConfigJson
            {
                FieldMap = new FieldMap
                {
                    Fields = new List<Field> { fieldConfig }
                },
                TypeMap = typeMap
            };

            var exportIssuesSummary = new ExportIssuesSummary();

            // Act
            var result = FieldMapperUtils.MapValue(revision, itemSource, itemTarget, config, exportIssuesSummary);

            // Assert
            Assert.IsTrue(result.Item1);
            Assert.AreNotEqual("Active", result.Item2);
            Assert.AreEqual("Open", result.Item2); // no mapping should have taken place

        }

        [Test]
        public void MapValue_WithExistingFieldAndNoMapping_ReturnsTrueAndOriginalValue()
        {
            // Arrange
            string itemSource = "FieldWithNoMapping";
            string itemTarget = "Target";
            var revision = new JiraRevision(_item)
            {
                Fields = new Dictionary<string, object>
                {
                    { "Priority", "High" },
                    { "Status", "Open" },
                    { "FieldWithNoMapping", "SourceValue" }
                }
            };

            var exportIssuesSummary = new ExportIssuesSummary();

            // Act
            var result = FieldMapperUtils.MapValue(revision, itemSource, itemTarget, _config, exportIssuesSummary);

            // Assert
            Assert.IsTrue(result.Item1);
            Assert.AreEqual("SourceValue", result.Item2);
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

        private IJiraProvider CreateJiraProvider(JObject remoteIssue = null)
        {
            IJiraProvider provider = Substitute.For<IJiraProvider>();
            provider.GetSettings().ReturnsForAnyArgs(CreateJiraSettings());
            provider.DownloadIssue(default).ReturnsForAnyArgs(remoteIssue ?? CreateRemoteIssueJObject());

            return provider;
        }

        private JObject CreateRemoteIssueJObject(string workItemType = "Bug", string issueKey = "issue_key")
        {
            var issueType = JObject.Parse("{ 'issuetype': {'name': '"+ workItemType +"'}}");
            var renderedFields = JObject.Parse("{ 'custom_field_name': 'SomeValue', 'description': 'RenderedDescription' }");

            return new JObject
            {
                { "fields", issueType },
                { "renderedFields", renderedFields },
                { "key", issueKey }
            };

        }
    }
}