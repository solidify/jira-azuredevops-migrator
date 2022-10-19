using NUnit.Framework;

using AutoFixture.AutoNSubstitute;
using AutoFixture;
using System;
using Newtonsoft.Json.Linq;
using NSubstitute;
using System.Collections.Generic;
using JiraExport;
using Common.Config;
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
            JiraSettings settings = new JiraSettings("userID", "pass", "url", "project");
            settings.SprintField = "SprintField";
            provider.GetSettings().ReturnsForAnyArgs(settings);

            JiraItem item = JiraItem.CreateFromRest(issueKey, provider);
            var revision = new JiraRevision(item);

            revision.Fields = new Dictionary<string, object>();
            revision.Fields["summary"] = revisionSummary;
            revision.Fields["priority"] = "High";
            revision.Fields["status"] = "Done";
            revision.Fields["issuetype"] = issueType;
            revision.Fields["custom_field_name$Rendered"] = "RenderedValueHere";
            revision.Fields["description$Rendered"] = "<h>https://example.com</h><span class=\"image-wrap\">span_text<img https://abc.com />image_alt</span><a href=https://123.com class=\"user-hover\" link_meta>link_text</a>";

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
        public void When_calling_map_remaining_work_with_null_arguments_Then_and_exception_is_thrown()
        {
            Assert.Throws<ArgumentNullException>(() => { FieldMapperUtils.MapRemainingWork(null); });
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
            Assert.AreEqual(sprintPath[sprintPath.Length - 1], output);
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


            var actualOutput = FieldMapperUtils.MapValue(jiraRevision, "priority", "priority", configJson);

            Assert.Multiple(() =>
            {
                Assert.That(actualOutput.Item1, Is.True);
                Assert.That(actualOutput.Item2, Is.EqualTo("High"));
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


            var actualOutput = FieldMapperUtils.MapValue(jiraRevision, "emtpy", "empty", configJson);

            Assert.Multiple(() =>
            {
                Assert.That(actualOutput.Item1, Is.False);
                Assert.That(actualOutput.Item2, Is.Null);
            });
        }

        [Test]
        public void When_calling_map_value_with_null_arguments_Then_and_exception_is_thrown()
        {
            Assert.Throws<ArgumentNullException>(() => { FieldMapperUtils.MapValue(null, null, null, null); });
        }

        [Test]
        public void When_calling_correct_rendered_html_value_with_empty_string_arg_Then_an_exception_is_thrown()
        {
            var provider = _fixture.Freeze<IJiraProvider>();
            provider.DownloadIssue(default).Returns(new JObject());
            var revision = _fixture.Freeze<JiraRevision>();

            Assert.That(() => FieldMapperUtils.CorrectRenderedHtmlvalue("", revision), Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void When_calling_correct_rendered_html_value_with_valid_args_Then_expected_output_is_returned()
        {
            string issueKey = "issue_key";
            string summary = "My Summary";

            JiraRevision revision = MockRevisionWithParentItem(issueKey, summary);

            RevisionAction<JiraAttachment> revisionAction = new RevisionAction<JiraAttachment>();
            JiraAttachment attachment = new JiraAttachment();
            attachment.Url = "https://example.com";
            revisionAction.Value = attachment;

            revision.AttachmentActions = new List<RevisionAction<JiraAttachment>>();
            revision.AttachmentActions.Add(revisionAction);

            string output = FieldMapperUtils.CorrectRenderedHtmlvalue("" +
                "<h>https://example.com</h>" +
                "<span class=\"image-wrap\">span_text<img https://abc.com />image_alt</span>" +
                "<a href=https://123.com class=\"user-hover\" link_meta>link_text</a>",
                revision);
            string expected = "<h>https://example.com</h>" +
                "<img https://abc.com />" +
                "link_text";
            Assert.AreEqual(expected, output);
        }

        [Test]
        public void When_calling_map_correct_rendered_html_value_with_null_arguments_Then_and_exception_is_thrown()
        {
            Assert.Throws<ArgumentNullException>(() => { FieldMapperUtils.CorrectRenderedHtmlvalue(null, null); });
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
            JiraAttachment attachment = new JiraAttachment();
            attachment.Url = "https://example.com";
            revisionAction.Value = attachment;

            jiraRevision.AttachmentActions = new List<RevisionAction<JiraAttachment>>();
            jiraRevision.AttachmentActions.Add(revisionAction);


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
            JiraAttachment attachment = new JiraAttachment();
            attachment.Url = "https://example.com";
            revisionAction.Value = attachment;

            jiraRevision.AttachmentActions = new List<RevisionAction<JiraAttachment>>();
            jiraRevision.AttachmentActions.Add(revisionAction);


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


    }
}