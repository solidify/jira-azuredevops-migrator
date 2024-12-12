using AutoFixture;
using AutoFixture.AutoNSubstitute;
using Common.Config;
using JiraExport;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Migration.Jira_Export.Tests.RevisionUtils
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class FieldMapperUtilsTests
    {
        // use auto fixture to help mock and instantiate with dummy data with nsubsitute. 
        private Fixture _fixture;

        private JiraRevision MockRevisionWithParentItem(string issueKey, string revisionSummary)
        {
            var provider = _fixture.Freeze<IJiraProvider>();

            var issueType = JObject.Parse(@"{ 'issuetype': {'name': 'Story'}}");
            var renderedFields = JObject.Parse("{ 'custom_field_name': 'SomeValue', 'description': 'RenderedDescription' }");
            JObject remoteIssue = new JObject
            {
                { "fields", issueType },
                { "renderedFields", renderedFields },
                { "key", issueKey }
            };

            provider.DownloadIssue(default).ReturnsForAnyArgs(remoteIssue);
            JiraSettings settings = new JiraSettings("userID", "pass", "token", "url", "project")
            {
                SprintField = "SprintField"
            };
            provider.GetSettings().ReturnsForAnyArgs(settings);

            JiraItem item = JiraItem.CreateFromRest(issueKey, provider);
            var revision = new JiraRevision(item)
            {
                Fields = new Dictionary<string, object>
                {
                    ["summary"] = revisionSummary,
                    ["priority"] = "High",
                    ["status"] = "Done",
                    ["issuetype"] = issueType,
                    ["custom_field_name$Rendered"] = "RenderedValueHere",
                    ["description$Rendered"] = "<h>https://example.com</h><span class=\"image-wrap\">span_text<img https://abc.com />image_alt</span><a href=https://123.com class=\"user-hover\" link_meta>link_text</a>"
                }
            };

            return revision;
        }

        [SetUp]
        public void Setup()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoNSubstituteCustomization());
        }

        [Test]
        public void When_calling_map_remaining_work_with_valid_args_Then_expected_output_is_returned()
        {
            object output = FieldMapperUtils.MapRemainingWork("36000");

            Assert.AreEqual(10, output);
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase("Invalid")]
        public void When_calling_map_remaining_work_with_invalid_arguments_Then_null_is_returned(string value)
        {
            object output = FieldMapperUtils.MapRemainingWork(value);

            Assert.AreEqual(null, output);
        }

        [Test]
        public void When_calling_map_title_with_empty_args_Then_null_is_returned()
        {
            (bool, object) expected = (false, null);
            var provider = _fixture.Freeze<IJiraProvider>();
            provider.DownloadIssue(default).Returns(new JObject());

            var revision = _fixture.Freeze<JiraRevision>();


            (bool, object) output = FieldMapperUtils.MapTitle(revision);

            Assert.AreEqual(expected, output);
        }

        [Test]
        public void When_calling_map_title_with_valid_args_Then_expected_output_is_returned()
        {
            string issueKey = "issue_key";
            string summary = "My Summary";

            JiraRevision revision = MockRevisionWithParentItem(issueKey, summary);
            (bool, object) expected = (true, string.Format("[{0}] {1}", issueKey, summary));

            (bool, object) output = FieldMapperUtils.MapTitle(revision);

            Assert.AreEqual(expected, output);
        }

        [Test]
        public void When_calling_map_title_without_key_with_valid_args_Then_expected_output_is_returned()
        {
            string issueKey = "issue_key";
            string summary = "My Summary";

            JiraRevision revision = MockRevisionWithParentItem(issueKey, summary);


            (bool, object) expected = (true, summary);

            (bool, object) output = FieldMapperUtils.MapTitleWithoutKey(revision);

            Assert.AreEqual(expected, output);
        }

        [Test]
        public void When_calling_map_title_without_key_with_empty_args_Then_null_is_returned()
        {
            (bool, object) expected = (false, null);
            var provider = _fixture.Freeze<IJiraProvider>();
            provider.DownloadIssue(default).Returns(new JObject());
            var revision = _fixture.Freeze<JiraRevision>();

            (bool, object) output = FieldMapperUtils.MapTitleWithoutKey(revision);

            Assert.AreEqual(expected, output);
        }

        [Test]
        public void When_calling_map_title_with_null_arguments_Then_and_exception_is_thrown()
        {
            Assert.Throws<ArgumentNullException>(() => { FieldMapperUtils.MapTitleWithoutKey(null); });
        }

        [Test]
        public void When_calling_map_tags_with_empty_string_arg_Then_null_is_returned()
        {
            object output = FieldMapperUtils.MapTags("");
            Assert.AreEqual(string.Empty, output);
        }

        [Test]
        public void When_calling_map_tags_with_valid_args_Then_expected_output_is_returned()
        {
            string[] tags = { "TAG_A", "TAG_B", "TAG_C" };
            object output = FieldMapperUtils.MapTags(string.Join(" ", tags));
            Assert.AreEqual(string.Join(";", tags), output);
        }

        [Test]
        public void When_calling_map_tags_with_null_arguments_Then_and_exception_is_thrown()
        {
            Assert.Throws<ArgumentNullException>(() => { FieldMapperUtils.MapTags(null); });
        }

        [Test]
        public void When_calling_map_array_with_empty_string_arg_Then_null_is_returned()
        {
            object actualResult = FieldMapperUtils.MapArray("");

            Assert.That(actualResult, Is.Null);

        }

        [Test]
        public void When_calling_map_array_with_valid_args_Then_expected_output_is_returned()
        {
            string[] tags = { "ELEM_A", "ELEM_B", "ELEM_C" };
            object output = FieldMapperUtils.MapArray(string.Join(",", tags));
            Assert.AreEqual(string.Join(";", tags), output);
        }

        [Test]
        public void When_calling_map_array_with_null_arguments_Then_and_exception_is_thrown()
        {
            Assert.Throws<ArgumentNullException>(() => { FieldMapperUtils.MapArray(null); });
        }

        [Test]
        public void When_calling_map_sprint_with_empty_string_arg_Then_null_is_returned()
        {
            object actualResult = FieldMapperUtils.MapSprint("");

            Assert.That(actualResult, Is.Null);

        }

        [Test]
        public void When_calling_map_sprint_with_valid_args_Then_expected_output_is_returned()
        {
            string[] sprintPath = { "Base", "Segment", "Sprint" };
            object output = FieldMapperUtils.MapSprint(string.Join(",", sprintPath));
            Assert.AreEqual(sprintPath[^1], output);
        }

        [Test]
        public void When_calling_map_sprint_with_invalid_azdo_chars_Then_expected_output_is_returned()
        {
            string[] sprintPath = { "*#/Base", "Seg*#/ment", "Sprint*#/" };
            string expected = "Sprint";
            object output = FieldMapperUtils.MapSprint(string.Join(",", sprintPath));
            Assert.AreEqual(expected, output);
        }

        [Test]
        public void When_calling_map_sprint_with_full_sprint_object_as_str_Then_expected_output_is_returned()
        {
            string sprintPath = "com.atlassian.greenhopper.service.sprint.Sprint@7c6e1967[id=442906,rapidViewId"
                + "=187524,state=ACTIVE,name=LMS 2024_05,startDate=2024-10-14T00:00:00.000Z,endDate=2024-11-01T"
                + "23:00:00.000Z,completeDate=<null>,activatedDate=2024-10-13T18:14:54.334Z,sequence=449386,goa"
                + "l=,synced=false,autoStartStop=false,incompleteIssuesDestinationId=<null>]";
            string expectedOutput = "LMS 2024_05";
            object output = FieldMapperUtils.MapSprint(sprintPath);
            Assert.AreEqual(expectedOutput, output);
        }

        [Test]
        public void When_calling_map_value_with_valid_args_Then_expected_output_is_returned()
        {

            var configJson = _fixture.Create<ConfigJson>();

            configJson.TypeMap.Types = new List<Common.Config.Type>() { new Common.Config.Type() { Source = "Story", Target = "Story" } };
            configJson.FieldMap.Fields = new List<Common.Config.Field>()
            {
                new Common.Config.Field()
            {
                Source = "priority", Target = "Microsoft.VSTS.Common.Priority",
                Mapping = new Common.Config.Mapping
                {
                    Values = new List<Common.Config.Value>
                    {
                        new Common.Config.Value
                    {
                        Source = "High", Target = "1"
                        }
                    }
                    }
                }
            };

            var jiraRevision = MockRevisionWithParentItem("issue_key", "My Summary");

            var exportIssuesSummary = new ExportIssuesSummary();

            var actualOutput = FieldMapperUtils.MapValue(jiraRevision, "priority", "priority", configJson, exportIssuesSummary);

            Assert.Multiple(() =>
            {
                Assert.That(actualOutput.Item1, Is.True);
                Assert.That(actualOutput.Item2, Is.EqualTo("High"));
            });
        }

        [Test]
        public void When_calling_map_value_with_valid_args_and_null_sourcevalue_Then_expected_output_is_returned()
        {

            var configJson = _fixture.Create<ConfigJson>();

            configJson.TypeMap.Types = new List<Common.Config.Type>() { new Common.Config.Type() { Source = "Story", Target = "Story" } };
            configJson.FieldMap.Fields = new List<Common.Config.Field>()
            {
                new Common.Config.Field()
                    {
                        Source = "resolution", Target = "System.Reason",
                        Mapping = new Common.Config.Mapping
                        {
                            Values = new List<Common.Config.Value>
                            {
                                new Common.Config.Value
                            {
                                Source = "Fixed", Target = "Resolved"
                                }
                            }
                            }
                        }
                    };

            var jiraRevision = MockRevisionWithParentItem("issue_key", "My Summary");
            // Ensure a null value is added to the revision
            jiraRevision.Fields.Add("resolution", null);

            var exportIssuesSummary = new ExportIssuesSummary();

            var actualOutput = FieldMapperUtils.MapValue(jiraRevision, "resolution", "System.Reason", configJson, exportIssuesSummary);

            Assert.Multiple(() =>
            {
                Assert.That(actualOutput.Item1, Is.True);
                Assert.That(actualOutput.Item2, Is.Null);
            });
        }

        [Test]
        public void When_calling_map_value_with_missing_args_Then_false_and_null_is_returned()
        {

            var configJson = _fixture.Create<ConfigJson>();

            configJson.TypeMap.Types = new List<Common.Config.Type>() { new Common.Config.Type() { Source = "Story", Target = "Story" } };
            configJson.FieldMap.Fields = new List<Common.Config.Field>()
            {
                new Common.Config.Field()
            {
                Source = "Whatever", Target = "Microsoft.VSTS.Common.Priority",
                Mapping = new Common.Config.Mapping
                {
                    Values = new List<Common.Config.Value>
                    {
                        new Common.Config.Value
                    {
                        Source = "High", Target = "1"
                        }
                    }
                    }
                }
            };

            var jiraRevision = MockRevisionWithParentItem("issue_key", "My Summary");

            var exportIssuesSummary = new ExportIssuesSummary();

            var actualOutput = FieldMapperUtils.MapValue(jiraRevision, "emtpy", "empty", configJson, exportIssuesSummary);

            Assert.Multiple(() =>
            {
                Assert.That(actualOutput.Item1, Is.False);
                Assert.That(actualOutput.Item2, Is.Null);
            });
        }

        [Test]
        public void When_calling_map_value_with_null_arguments_Then_and_exception_is_thrown()
        {
            Assert.Throws<ArgumentNullException>(() => { FieldMapperUtils.MapValue(null, null, null, null, null); });
        }

        [Test]
        public void When_calling_correct_rendered_html_value_with_empty_or_whitespace_string_arg_Then_the_description_is_mapped()
        {
            //Arrange
            var provider = _fixture.Freeze<IJiraProvider>();
            provider.DownloadIssue(default).Returns(new JObject());
            var revision = _fixture.Freeze<JiraRevision>();
            string description = string.Empty;

            //Act
            string output = FieldMapperUtils.CorrectRenderedHtmlvalue(description, revision, true);

            //Assert
            Assert.Multiple(() =>
                Assert.AreEqual(description, output)
            );
        }

        [Test]
        public void When_calling_correct_rendered_html_value_with_valid_args_Then_expected_output_is_returned()
        {
            string issueKey = "issue_key";
            string summary = "My Summary";

            JiraRevision revision = MockRevisionWithParentItem(issueKey, summary);

            RevisionAction<JiraAttachment> revisionAction = new RevisionAction<JiraAttachment>();
            JiraAttachment attachment = new JiraAttachment
            {
                Url = "https://example.com"
            };
            revisionAction.Value = attachment;

            revision.AttachmentActions = new List<RevisionAction<JiraAttachment>>
            {
                revisionAction
            };

            string output = FieldMapperUtils.CorrectRenderedHtmlvalue("" +
                "<h>https://example.com</h>" +
                "<span class=\"image-wrap\">span_text<img https://abc.com />image_alt</span>" +
                "<a href=https://123.com class=\"user-hover\" link_meta>link_text</a>",
                revision, true);
            string expected = "<h>https://example.com</h>" +
                "<img https://abc.com />" +
                "link_text";
            Assert.AreEqual(expected, output);
        }

        [Test]
        public void When_calling_map_correct_rendered_html_value_with_null_arguments_Then_and_exception_is_thrown()
        {
            Assert.Throws<ArgumentNullException>(() => { FieldMapperUtils.CorrectRenderedHtmlvalue(null, null, true); });
        }

        [Test]
        public void When_calling_map_rendered_value_with_valid_input_Then_expected_output_is_returned()
        {

            var sourceField = "description";
            var customFieldName = "custom_field_name";
            var configJson = _fixture.Create<ConfigJson>();

            var expectedOutput = "<h>https://example.com</h><img https://abc.com />link_text";

            configJson.TypeMap.Types = new List<Common.Config.Type>() { new Common.Config.Type() { Source = "Story", Target = "Story" } };
            configJson.FieldMap.Fields = new List<Common.Config.Field>()
            {
                new Common.Config.Field()
            {
                Source = sourceField, Target = "System.Description",
                Mapper = "MapRendered"

                }
            };

            var jiraRevision = MockRevisionWithParentItem("issue_key", "My Summary");

            RevisionAction<JiraAttachment> revisionAction = new RevisionAction<JiraAttachment>();
            JiraAttachment attachment = new JiraAttachment
            {
                Url = "https://example.com"
            };
            revisionAction.Value = attachment;

            jiraRevision.AttachmentActions = new List<RevisionAction<JiraAttachment>>
            {
                revisionAction
            };


            var actualOutput = FieldMapperUtils.MapRenderedValue(jiraRevision, sourceField, false, customFieldName, configJson);

            Assert.Multiple(() =>
            {
                Assert.That(actualOutput.Item1, Is.True);
                Assert.That(actualOutput.Item2, Is.Not.Empty);
                Assert.That(actualOutput.Item2, Is.EqualTo(expectedOutput));
            });

        }

        [Test]
        public void When_calling_map_rendered_value_with_invalid_input_Then_expected_false_and_null_is_returned()
        {

            var sourceField = "non_existing_field";
            var customFieldName = "custom_field_name";
            var configJson = _fixture.Create<ConfigJson>();
            configJson.TypeMap.Types = new List<Common.Config.Type>() { new Common.Config.Type() { Source = "Story", Target = "Story" } };
            configJson.FieldMap.Fields = new List<Common.Config.Field>() { new Common.Config.Field() };

            var jiraRevision = MockRevisionWithParentItem("issue_key", "My Summary");

            RevisionAction<JiraAttachment> revisionAction = new RevisionAction<JiraAttachment>();
            JiraAttachment attachment = new JiraAttachment
            {
                Url = "https://example.com"
            };
            revisionAction.Value = attachment;

            jiraRevision.AttachmentActions = new List<RevisionAction<JiraAttachment>>
            {
                revisionAction
            };


            var actualOutput = FieldMapperUtils.MapRenderedValue(jiraRevision, sourceField, false, customFieldName, configJson);

            Assert.Multiple(() =>
            {
                Assert.That(actualOutput.Item1, Is.False);
                Assert.That(actualOutput.Item2, Is.Null);
            });

        }

        [Test]
        public void When_calling_map_rendered_value_with_null_arguments_Then_and_exception_is_thrown()
        {
            Assert.Throws<ArgumentNullException>(() => { FieldMapperUtils.MapRenderedValue(null, null, false, null, null); });
        }

        [TestCase("2|hzyxfj:", 1088341183.0)]
        [TestCase("2|hzyxfj:rx4", 1088341183.36184)]
        public void When_calling_map_lexorank_value_with_valid_argument_Then_the_correct_value_is_returned(string lexoRank, decimal expectedRank)
        {
            Assert.That(FieldMapperUtils.MapLexoRank(lexoRank), Is.EqualTo(expectedRank));
        }

        [TestCase(null)]
        [TestCase("Hello World")]
        [TestCase("2|jghhdf kjh dkjh sd")]
        [TestCase("2|hzyxfj:rx4:bt5")]
        public void When_calling_map_lexorank_value_with_invalid_argument_Then_max_value_is_returned(string lexoRank)
        {
            Assert.That(FieldMapperUtils.MapLexoRank(lexoRank), Is.EqualTo(decimal.MaxValue));
        }

        [Test]
        public void
            When_calling_map_lexorank_value_with_over_precise_argument_Then_the_correct_devops_precision_value_is_returned()
        {
            Assert.That(FieldMapperUtils.MapLexoRank("0|hzyxfj:hzyxfj"), Is.EqualTo(1088341183.1088341M));
        }
    }
}