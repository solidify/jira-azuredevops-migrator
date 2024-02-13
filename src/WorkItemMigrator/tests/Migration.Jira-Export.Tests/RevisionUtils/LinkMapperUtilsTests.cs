﻿using AutoFixture;
using AutoFixture.AutoNSubstitute;
using Common.Config;
using JiraExport;
using Migration.Common.Config;
using Migration.WIContract;
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
    public class LinkMapperUtilsTests
    {
        // use auto fixture to help mock and instantiate with dummy data with nsubsitute. 
        private Fixture _fixture;

        private JiraRevision MockRevisionWithParentItem(string issueKey, string revisionSummary)
        {
            var provider = _fixture.Freeze<IJiraProvider>();

            JObject remoteIssue = new JObject
            {
                { "fields", new JObject() },
                { "renderedFields", new JObject() },
                { "key", issueKey }
            };

            provider.DownloadIssue(default).ReturnsForAnyArgs(remoteIssue);
            JiraSettings settings = new JiraSettings("userID", "pass", "token", "url", "project");
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

        //public static void AddSingleLink(JiraRevision r, List<WiLink> links, string field, string type, ConfigJson config)

        [Test]
        public void When_calling_add_single_link_with_empty_string_arg_Then_an_exception_is_thrown()
        {
            var provider = _fixture.Freeze<IJiraProvider>();
            provider.DownloadIssue(default).Returns(new JObject());
            var revision = _fixture.Freeze<JiraRevision>();

            Assert.That(() => LinkMapperUtils.AddSingleLink(revision, new List<WiLink>(), "", "", new ConfigJson()), Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void When_calling_add_single_link_with_valid_field_Then_a_link_is_added()
        {
            string issueKey = "issue_key";
            string summary = "My Summary";
            string targetId = "Target_ID";
            string targetWiType = "Target_Wi_Type";
            string child = "Child";
            string epicChild = "epic child";

            JiraRevision revision = MockRevisionWithParentItem(issueKey, summary);

            Link link = new Link();
            link.Source = child;
            link.Target = targetWiType;

            ConfigJson configJson = new ConfigJson();

            configJson.LinkMap = new LinkMap();
            configJson.LinkMap.Links = new List<Link>
            {
                link
            };

            List<WiLink> links = new List<WiLink>();

            revision.Fields[epicChild] = targetId;

            LinkMapperUtils.AddSingleLink(revision, links, epicChild, child, configJson);

            Assert.Multiple(() =>
            {
                Assert.AreEqual(ReferenceChangeType.Added, links[0].Change);
                Assert.AreEqual(issueKey, links[0].SourceOriginId);
                Assert.AreEqual(targetId, links[0].TargetOriginId);
                Assert.AreEqual(targetWiType, links[0].WiType);
            });


        }

        [Test]
        public void When_calling_add_single_link_with_null_field_Then_no_link_is_added()
        {
            string issueKey = "issue_key";
            string summary = "My Summary";
            string targetWiType = "Target_Wi_Type";
            string child = "Child";
            string epicChild = "epic child";

            JiraRevision revision = MockRevisionWithParentItem(issueKey, summary);

            Link link = new Link();
            link.Source = child;
            link.Target = targetWiType;

            ConfigJson configJson = new ConfigJson();

            configJson.LinkMap = new LinkMap();
            configJson.LinkMap.Links = new List<Link>
            {
                link
            };

            List<WiLink> links = new List<WiLink>();

            revision.Fields[epicChild] = null;

            LinkMapperUtils.AddSingleLink(revision, links, epicChild, child, configJson);

            Assert.IsEmpty(links);
        }

        [Test]
        public void When_calling_add_single_link_with_null_arguments_Then_and_exception_is_thrown()
        {
            Assert.Throws<ArgumentNullException>(() => { LinkMapperUtils.AddSingleLink(null, null, null, null, null); });
        }

        [Test]
        public void When_calling_add_remove_single_link_with_empty_string_arg_Then_an_exception_is_thrown()
        {
            var provider = _fixture.Freeze<IJiraProvider>();
            provider.DownloadIssue(default).Returns(new JObject());
            var revision = _fixture.Freeze<JiraRevision>();

            Assert.That(() => LinkMapperUtils.AddRemoveSingleLink(revision, new List<WiLink>(), "", "", new ConfigJson()), Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void When_calling_add_remove_single_link_with_valid_field_Then_a_link_is_added()
        {
            string issueKey = "issue_key";
            string summary = "My Summary";
            string targetId = "Target_ID";
            string targetWiType = "Target_Wi_Type";
            string child = "Child";
            string epicChild = "epic child";

            JiraRevision revision = MockRevisionWithParentItem(issueKey, summary);

            Link link = new Link();
            link.Source = child;
            link.Target = targetWiType;

            ConfigJson configJson = new ConfigJson();

            configJson.LinkMap = new LinkMap();
            configJson.LinkMap.Links = new List<Link>
            {
                link
            };

            List<WiLink> links = new List<WiLink>();

            revision.Fields[epicChild] = targetId;

            LinkMapperUtils.AddRemoveSingleLink(revision, links, epicChild, child, configJson);


            Assert.Multiple(() =>
            {
                Assert.AreEqual(ReferenceChangeType.Added, links[0].Change);
                Assert.AreEqual(issueKey, links[0].SourceOriginId);
                Assert.AreEqual(targetId, links[0].TargetOriginId);
                Assert.AreEqual(targetWiType, links[0].WiType);
            });


        }

        [Test]
        public void When_calling_add_remove_single_link_with_valid_field_Then_a_link_is_removed()
        {
            string issueKey = "issue_key";
            string summary = "My Summary";
            string targetId = "Target_ID";
            string targetWiType = "Target_Wi_Type";
            string child = "Child";
            string epicChild = "epic child";

            JiraRevision revision = MockRevisionWithParentItem(issueKey, summary);
            JiraRevision revision2 = MockRevisionWithParentItem(issueKey, summary);
            JiraRevision revision3 = MockRevisionWithParentItem(issueKey, summary);

            revision.Index = 1;
            revision.Fields[epicChild] = null;
            revision2.Fields[epicChild] = targetId;
            revision3.Fields[epicChild] = targetId;
            revision.ParentItem.Revisions.Insert(0, revision2);
            revision2.ParentItem.Revisions.Insert(0, revision);
            revision.ParentItem.Revisions.Insert(0, revision);

            Link link = new Link();
            link.Source = child;
            link.Target = targetWiType;

            ConfigJson configJson = new ConfigJson();

            configJson.LinkMap = new LinkMap();
            configJson.LinkMap.Links = new List<Link>
            {
                link
            };

            List<WiLink> links = new List<WiLink>();

            LinkMapperUtils.AddRemoveSingleLink(revision, links, epicChild, child, configJson);

            Assert.Multiple(() =>
            {
                Assert.AreEqual(ReferenceChangeType.Removed, links[0].Change);
                Assert.AreEqual(issueKey, links[0].SourceOriginId);
                Assert.AreEqual(targetId, links[0].TargetOriginId);
                Assert.AreEqual(targetWiType, links[0].WiType);
            });
        }

        [Test]
        public void When_calling_add_remove_single_link_with_null_arguments_Then_and_exception_is_thrown()
        {
            Assert.Throws<ArgumentNullException>(() => { LinkMapperUtils.AddRemoveSingleLink(null, null, null, null, null); });
        }

        [Test]
        public void When_calling_map_epic_child_link_with_empty_string_arg_Then_an_exception_is_thrown()
        {
            var provider = _fixture.Freeze<IJiraProvider>();
            provider.DownloadIssue(default).Returns(new JObject());
            var revision = _fixture.Freeze<JiraRevision>();

            Assert.That(() => LinkMapperUtils.MapEpicChildLink(revision, new List<WiLink>(), "", "", new ConfigJson()), Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void When_calling_map_epic_child_link_with_valid_field_Then_a_link_is_added()
        {
            // issueKey must be > targetId for a link to be generated
            string issueKey = "9";
            string targetId = "8";
            string summary = "My Summary";
            string targetWiType = "Target_Wi_Type";
            string child = "Child";
            string epicChild = "epic child";

            JiraRevision revision = MockRevisionWithParentItem(issueKey, summary);

            Link link = new Link();
            link.Source = child;
            link.Target = targetWiType;

            ConfigJson configJson = new ConfigJson();

            configJson.LinkMap = new LinkMap();
            configJson.LinkMap.Links = new List<Link>
            {
                link
            };

            List<WiLink> links = new List<WiLink>();

            revision.Fields[epicChild] = targetId;

            LinkMapperUtils.MapEpicChildLink(revision, links, epicChild, child, configJson);

            Assert.Multiple(() =>
            {
                Assert.AreEqual(ReferenceChangeType.Added, links[0].Change);
                Assert.AreEqual(issueKey, links[0].SourceOriginId);
                Assert.AreEqual(targetId, links[0].TargetOriginId);
                Assert.AreEqual(targetWiType, links[0].WiType);
            });
        }

        [Test]
        public void When_calling_map_epic_child_link_with_null_arguments_Then_and_exception_is_thrown()
        {
            Assert.Throws<ArgumentNullException>(() => { LinkMapperUtils.MapEpicChildLink(null, null, null, null, null); });
        }

    }
}