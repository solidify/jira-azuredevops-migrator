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

            JObject remoteIssue = new JObject();
            remoteIssue.Add("fields", new JObject());
            remoteIssue.Add("renderedFields", new JObject());
            remoteIssue.Add("key", issueKey);

            provider.DownloadIssue(default).ReturnsForAnyArgs(remoteIssue);
            JiraSettings settings = new JiraSettings("userID", "pass", "url", "project");
            settings.SprintField = "SprintField";
            provider.GetSettings().ReturnsForAnyArgs(settings);

            JiraItem item = JiraItem.CreateFromRest(issueKey, provider);

            var revision = new JiraRevision(item);
            revision.Fields = new Dictionary<string, object>();
            revision.Fields["summary"] = revisionSummary;

            return revision;
        }

        [SetUp]
        public void Setup()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoNSubstituteCustomization() { });
        }

        [Test]
        public void When_calling_map_remaining_work_with_valid_args_Then_expected_output_is_returned()
        {
            object output = FieldMapperUtils.MapRemainingWork("36000");
            Assert.AreEqual (output, 10);
        }

        [Test]
        public void When_calling_map_title_with_empty_args_Then_null_is_returnedt()
        {
            var provider = _fixture.Freeze<IJiraProvider>();
            provider.DownloadIssue(default).Returns(new JObject());
            var revision = _fixture.Freeze<JiraRevision>();

            (bool, object) output = FieldMapperUtils.MapTitle(revision);
            (bool, object) expected = (false, null);
            Assert.AreEqual(output, expected);
        }

        [Test]
        public void When_calling_map_title_with_valid_args_Then_expected_output_is_returned()
        {
            string issueKey = "issue_key";
            string summary = "My Summary";

            JiraRevision revision = MockRevisionWithParentItem(issueKey, summary);

            (bool, object) output = FieldMapperUtils.MapTitle(revision);
            (bool, object) expected = (true, String.Format("[{0}] {1}", issueKey, summary));
            Assert.AreEqual(output, expected);
        }

        [Test]
        public void When_calling_map_title_without_key_with_valid_args_Then_expected_output_is_returned()
        {
            string issueKey = "issue_key";
            string summary = "My Summary";

            JiraRevision revision = MockRevisionWithParentItem(issueKey, summary);

            (bool, object) output = FieldMapperUtils.MapTitleWithoutKey(revision);
            (bool, object) expected = (true, summary);
            Assert.AreEqual(output, expected);
        }

        [Test]
        public void When_calling_map_title_without_key_with_empty_args_Then_null_is_returnedt()
        {
            var provider = _fixture.Freeze<IJiraProvider>();
            provider.DownloadIssue(default).Returns(new JObject());
            var revision = _fixture.Freeze<JiraRevision>();

            (bool, object) output = FieldMapperUtils.MapTitleWithoutKey(revision);
            (bool, object) expected = (false, null);
            Assert.AreEqual(output, expected);
        }

        [Test]
        public void When_calling_map_tags_with_empty_string_arg_Then_null_is_returnedt()
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
        public void When_calling_map_array_with_empty_string_arg_Then_null_is_returnedt()
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
            Assert.AreEqual(output, sprintPath[sprintPath.Length-1]);
        }

    }
}