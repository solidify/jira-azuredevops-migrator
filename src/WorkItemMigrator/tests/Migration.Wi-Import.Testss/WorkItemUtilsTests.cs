using NUnit.Framework;

using AutoFixture.AutoNSubstitute;
using AutoFixture;
using System;
using WorkItemImport;
using Migration.WIContract;
using System.Collections.Generic;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using System.Threading.Tasks;
using NSubstitute;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace Migration.Wi_Import.Testss
{
    [TestFixture]
    public class WorkItemUtilsTests
    {
        // use auto fixiture to help mock and instantiate with dummy data with nsubsitute. 
        private Fixture _fixture;

        [SetUp]
        public void Setup()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoNSubstituteCustomization() { });
        }

        private bool MockedIsAttachmentMigratedDelegate(string _attOriginId, out int attWiId)
        {
            attWiId = 1;
            return true;
        }
        /*
        [Test]
        public void When_calling_correct_image_path_with_empty_args_Then_an_exception_is_thrown()
        {
            WorkItem wi = new WorkItem();
            WiItem wiItem = new WiItem();
            WiRevision rev = new WiRevision();
            string textField = "";

            WorkItemUtils.CorrectImagePath(wi, wiItem, rev, textField, true, MockedIsAttachmentMigratedDelegate);

            WorkItemUtils.EnsureAuthorFields(null);

            Assert.That(
                () => WorkItemUtils.CorrectImagePath(wi, wiItem, rev, textField, true, MockedIsAttachmentMigratedDelegate),
                Throws.InstanceOf<NullReferenceException>());
        }
        */

        [Test]
        public void When_calling_ensure_author_fields_with_empty_args_Then_an_exception_is_thrown()
        {
            WitClientWrapper wiUtils = new WitClientWrapper("https://dev.azure.com/solidify", "testproject");
            Assert.That(
                () => wiUtils.EnsureAuthorFields(null),
                Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void When_calling_ensure_author_fields_with_first_revision_Then_author_is_added_to_fields()
        {
            WiRevision rev = new WiRevision();
            rev.Fields = new List<WiField>();
            rev.Index = 0;
            rev.Author = "Firstname Lastname";

            WitClientWrapper wiUtils = new WitClientWrapper("https://dev.azure.com/solidify", "testproject");
            wiUtils.EnsureAuthorFields(rev);

            Assert.That(rev.Fields[0].ReferenceName, Is.EqualTo(WiFieldReference.CreatedBy));
            Assert.That(rev.Fields[0].Value, Is.EqualTo(rev.Author));
        }

        [Test]
        public void When_calling_ensure_author_fields_with_subsequent_revision_Then_author_is_added_to_fields()
        {
            WiRevision rev = new WiRevision();
            rev.Fields = new List<WiField>();
            rev.Index = 1;
            rev.Author = "Firstname Lastname";
            WitClientWrapper wiUtils = new WitClientWrapper("https://dev.azure.com/solidify", "testproject");
            wiUtils.EnsureAuthorFields(rev);

            Assert.That(rev.Fields[0].ReferenceName, Is.EqualTo(WiFieldReference.ChangedBy));
            Assert.That(rev.Fields[0].Value, Is.EqualTo(rev.Author));
        }

        [Test]
        public void When_calling_ensure_classification_fields_with_empty_args_Then_an_exception_is_thrown()
        {
            WitClientWrapper wiUtils = new WitClientWrapper("https://dev.azure.com/solidify", "testproject");
            Assert.That(
                () => wiUtils.EnsureClassificationFields(null),
                Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void When_calling_ensure_classification_fields_Then_areapath_and_iterationpath_are_added_to_fields()
        {
            WiRevision rev = new WiRevision();
            rev.Fields = new List<WiField>();

            WitClientWrapper wiUtils = new WitClientWrapper("https://dev.azure.com/solidify", "testproject");
            wiUtils.EnsureClassificationFields(rev);

            List<WiField> filteredForAreaPath = rev.Fields.FindAll(f => f.ReferenceName == WiFieldReference.AreaPath && f.Value == "");
            List<WiField> filteredForIterationPath = rev.Fields.FindAll(f => f.ReferenceName == WiFieldReference.IterationPath && f.Value == "");

            Assert.That(filteredForAreaPath.Count, Is.EqualTo(1));
            Assert.That(filteredForIterationPath.Count, Is.EqualTo(1));
        }

        [Test]
        public void When_calling_is_duplicate_work_item_link_with_empty_args_Then_an_exception_is_thrown()
        {
            WitClientWrapper wiUtils = new WitClientWrapper("https://dev.azure.com/solidify", "testproject");
            Assert.That(
                () => wiUtils.IsDuplicateWorkItemLink(null, null),
                Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void When_calling_is_duplicate_work_item_link_with_containing_link_Then_true_is_returned()
        {
            WorkItemRelation[] links = new WorkItemRelation[0];
            WorkItemRelation relatedLink = new WorkItemRelation();

            WitClientWrapper wiUtils = new WitClientWrapper("https://dev.azure.com/solidify", "testproject");
            bool isDuplicate = wiUtils.IsDuplicateWorkItemLink(links, relatedLink);

            var WitClient = _fixture.Freeze<WorkItemTrackingHttpClient>();

            // Mock WITClient
            WitClient.GetWorkItemAsync("", 0).Returns(Task.FromResult(new WorkItem()));


            Assert.That(isDuplicate, Is.EqualTo(true));
        }
    }
}