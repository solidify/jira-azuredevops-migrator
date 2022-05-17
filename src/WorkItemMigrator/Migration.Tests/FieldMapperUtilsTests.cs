using NUnit.Framework;

using JiraExport;
using AutoFixture.AutoNSubstitute;
using AutoFixture;
using System;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Common.Config;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Migration.Tests
{
    [TestFixture]
    public class FieldMapperUtilsTests
    {
        // use auto fixiture to help mock and instantiate with dummy data with nsubsitute. 
        private Fixture _fixture;

        private JiraRevision MockRevisionWithParentItem(string issueKey, string revisionSummary)
        {
            var provider = _fixture.Freeze<IJiraProvider>();

            var issueType = @"{ issuetype: {'name': 'Story'}}";
            JObject remoteIssue = new JObject
            {
                { "fields", JObject.Parse(issueType) },
                { "renderedFields", new JObject() },
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
            revision.Fields["issuetype"] = JObject.Parse(issueType);

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

            Assert.AreEqual(output, 10);
        }

        [Test]
        public void When_calling_map_title_with_empty_args_Then_null_is_returned()
        {
            (bool, object) expected = (false, null);
            var provider = _fixture.Freeze<IJiraProvider>();
            provider.DownloadIssue(default).Returns(new JObject());

            var revision = _fixture.Freeze<JiraRevision>();


            (bool, object) output = FieldMapperUtils.MapTitle(revision);

            Assert.AreEqual(output, expected);
        }

        [Test]
        public void When_calling_map_title_with_valid_args_Then_expected_output_is_returned()
        {
            string issueKey = "issue_key";
            string summary = "My Summary";

            JiraRevision revision = MockRevisionWithParentItem(issueKey, summary);
            (bool, object) expected = (true, String.Format("[{0}] {1}", issueKey, summary));

            (bool, object) output = FieldMapperUtils.MapTitle(revision);

            Assert.AreEqual(output, expected);
        }

        [Test]
        public void When_calling_map_title_without_key_with_valid_args_Then_expected_output_is_returned()
        {
            string issueKey = "issue_key";
            string summary = "My Summary";

            JiraRevision revision = MockRevisionWithParentItem(issueKey, summary);


            (bool, object) expected = (true, summary);

            (bool, object) output = FieldMapperUtils.MapTitleWithoutKey(revision);

            Assert.AreEqual(output, expected);
        }

        [Test]
        public void When_calling_map_title_without_key_with_empty_args_Then_null_is_returned()
        {
            (bool, object) expected = (false, null);
            var provider = _fixture.Freeze<IJiraProvider>();
            provider.DownloadIssue(default).Returns(new JObject());
            var revision = _fixture.Freeze<JiraRevision>();

            (bool, object) output = FieldMapperUtils.MapTitleWithoutKey(revision);

            Assert.AreEqual(output, expected);
        }

        [Test]
        public void When_calling_map_tags_with_empty_string_arg_Then_null_is_returned()
        {
            object output = FieldMapperUtils.MapTags("");
            Assert.AreEqual(output, null);
        }

        [Test]
        public void When_calling_map_tags_with_valid_args_Then_expected_output_is_returned()
        {
            string[] tags = { "TAG_A", "TAG_B", "TAG_C" };
            object output = FieldMapperUtils.MapTags(string.Join(" ", tags));
            Assert.AreEqual(output, string.Join(";", tags));
        }

        [Test]
        public void When_calling_map_array_with_empty_string_arg_Then_null_is_returned()
        {
            object output = FieldMapperUtils.MapArray("");
            Assert.AreEqual(output, null);
        }

        [Test]
        public void When_calling_map_array_with_valid_args_Then_expected_output_is_returned()
        {
            string[] tags = { "ELEM_A", "ELEM_B", "ELEM_C" };
            object output = FieldMapperUtils.MapArray(string.Join(",", tags));
            Assert.AreEqual(output, string.Join(";", tags));
        }

        [Test]
        public void When_calling_map_sprint_with_empty_string_arg_Then_null_is_returnedt()
        {
            object output = FieldMapperUtils.MapSprint("");
            Assert.AreEqual(output, null);
        }

        [Test]
        public void When_calling_map_sprint_with_valid_args_Then_expected_output_is_returned()
        {
            string[] sprintPath = { "Base", "Segment", "Sprint" };
            object output = FieldMapperUtils.MapSprint(string.Join(",", sprintPath));
            Assert.AreEqual(output, sprintPath[sprintPath.Length - 1]);
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


            var actualOutput = FieldMapperUtils.MapValue(jiraRevision, "priority", configJson);

            Assert.Multiple(() =>
            {
                Assert.That(actualOutput.Item1, Is.True);
                Assert.That(actualOutput.Item2, Is.EqualTo("1"));

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


            var actualOutput = FieldMapperUtils.MapValue(jiraRevision, "emtpy", configJson);

            Assert.Multiple(() =>
            {
                Assert.That(actualOutput.Item1, Is.False);
                Assert.That(actualOutput.Item2, Is.Null);
                
            });
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
            Assert.AreEqual(output, expected);
        }


    }
}