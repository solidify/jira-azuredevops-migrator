using NUnit.Framework;

using JiraExport;
using WorkItemImport;
using AutoFixture.AutoNSubstitute;
using AutoFixture;
using System;
using Microsoft.Extensions.CommandLineUtils;
using Migration.Common.Config;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Common.Config;
using Atlassian.Jira;

namespace Migration.Tests
{
    [TestFixture]
    public class FieldMapperUtilsTests
    {
        // use auto fixiture to help mock and instantiate with dummy data with nsubsitute. 
        private Fixture _fixture;

        [SetUp]
        public void Setup()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoNSubstituteCustomization() { });
        }

        [Test]
        public void When_calling_map_remaining_work_with_valid_args_Then_output_is_correct()
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
        public void When_calling_map_title_with_valid_args_Then_output_is_correct()
        {
            // WIP, Alexander
            var provider = _fixture.Freeze<IJiraProvider>();
            provider.DownloadIssue(default).Returns(new JObject());

            JiraItem item = JiraItem.CreateFromRest("issue_key", provider);

            var revision = _fixture.Freeze<JiraRevision>();
            revision.ParentItem.Returns(item);

            object output = FieldMapperUtils.MapTitle(revision);
            Assert.AreEqual(output, 10);
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

    }
}