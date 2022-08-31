﻿using NUnit.Framework;

using AutoFixture.AutoNSubstitute;
using AutoFixture;
using System;
using WorkItemImport;
using Migration.WIContract;
using System.Collections.Generic;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using System.Linq;

using Migration.Common;

namespace Migration.Wi_Import.Tests
{
    [TestFixture]
    public class WitClientUtilsTests
    {
        private class MockedWitClientWrapper : IWitClientWrapper
        {
            private int _wiIdCounter = 1;
            public Dictionary<int, WorkItem> _wiCache = new Dictionary<int, WorkItem>();

            public MockedWitClientWrapper()
            {

            }

            public WorkItem CreateWorkItem(string wiType)
            {
                WorkItem workItem = new WorkItem();
                workItem.Id = _wiIdCounter;
                workItem.Url = $"https://example/workItems/{_wiIdCounter}";
                workItem.Fields[WiFieldReference.WorkItemType] = wiType;
                workItem.Relations = new List<WorkItemRelation>();
                _wiCache[_wiIdCounter] = (workItem);
                _wiIdCounter++;
                return workItem;
            }

            public WorkItem GetWorkItem(int wiId)
            {
                return _wiCache[wiId];
            }

            public WorkItem UpdateWorkItem(JsonPatchDocument patchDocument, int workItemId)
            {
                WorkItem wi = _wiCache[workItemId];
                foreach(JsonPatchOperation op in patchDocument)
                {
                    if(op.Operation == Operation.Add)
                    {
                        if (op.Path.StartsWith("/fields/"))
                        {
                            string field = op.Path.Replace("/fields/", "");
                            wi.Fields[field] = op.Value;
                        }
                        else if (op.Path.StartsWith("/relations/"))
                        {
                            string rel = op.Value.GetType().GetProperty("rel")?.GetValue(op.Value, null).ToString();
                            string url = op.Value.GetType().GetProperty("url")?.GetValue(op.Value, null).ToString();
                            object attributes = op.Value.GetType().GetProperty("attributes")?.GetValue(op.Value, null);
                            string comment = attributes.GetType().GetProperty("comment")?.GetValue(attributes, null).ToString();

                            WorkItemRelation wiRelation = new WorkItemRelation();
                            wiRelation.Rel = rel;
                            wiRelation.Url = url;
                            wiRelation.Attributes = new Dictionary<string, object>{ { "comment", comment } };

                            wi.Relations.Add(wiRelation);
                        }
                    }
                    else if (op.Operation == Operation.Remove) {
                        if (op.Path.StartsWith("/fields/"))
                        {
                            string field = op.Path.Replace("/fields/", "");
                            wi.Fields[field] = "";
                        }
                        else if (op.Path.StartsWith("/relations/"))
                        {
                            WorkItemRelation referenceRelation = op.Value as WorkItemRelation;
                            WorkItemRelation found = wi.Relations.SingleOrDefault(a => a.Rel == referenceRelation.Rel && a.Url == referenceRelation.Url);
                            if(found != default(WorkItemRelation))
                            {
                                wi.Relations.Remove(op.Value as WorkItemRelation);
                            }
                        }
                    }
                }
                return wi;
            }

            public TeamProject GetProject(string projectId)
            {
                TeamProject tp = new TeamProject();
                Guid projGuid;
                if(Guid.TryParse(projectId, out projGuid))
                {
                    tp.Id = projGuid;
                }
                else
                {
                    tp.Id = Guid.NewGuid();
                    tp.Name = projectId;
                }
                return tp;
            }

            public List<WorkItemRelationType> GetRelationTypes()
            {
                WorkItemRelationType hierarchyForward = new WorkItemRelationType
                {
                    ReferenceName = "System.LinkTypes.Hierarchy-Forward"
                };
                WorkItemRelationType hierarchyReverse = new WorkItemRelationType
                {
                    ReferenceName = "System.LinkTypes.Hierarchy-Reverse"
                };

                List<WorkItemRelationType> outList = new List<WorkItemRelationType>();
                outList.Add(hierarchyForward);
                outList.Add(hierarchyReverse);
                return outList;
            }

            public AttachmentReference CreateAttachment(string filePath)
            {
                AttachmentReference att = new AttachmentReference();
                att.Id = Guid.NewGuid();
                att.Url = "https://example.com";
                return att;
            }
        }
        private bool MockedIsAttachmentMigratedDelegateTrue(string _attOriginId, out string attWiId)
        {
            attWiId = "1";
            return true;
        }

        private bool MockedIsAttachmentMigratedDelegateFalse(string _attOriginId, out string attWiId)
        {
            attWiId = "1";
            return false;
        }

        // use auto fixture to help mock and instantiate with dummy data with nsubsitute. 
        private Fixture _fixture;

        [SetUp]
        public void Setup()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoNSubstituteCustomization() { });
        }

        [Test]
        public void When_calling_ensure_author_fields_with_empty_args_Then_an_exception_is_thrown()
        {
            MockedWitClientWrapper witClientWrapper = new MockedWitClientWrapper();
            WitClientUtils wiUtils = new WitClientUtils(witClientWrapper);
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

            MockedWitClientWrapper witClientWrapper = new MockedWitClientWrapper();
            WitClientUtils wiUtils = new WitClientUtils(witClientWrapper);
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
            MockedWitClientWrapper witClientWrapper = new MockedWitClientWrapper();
            WitClientUtils wiUtils = new WitClientUtils(witClientWrapper);
            wiUtils.EnsureAuthorFields(rev);

            Assert.That(rev.Fields[0].ReferenceName, Is.EqualTo(WiFieldReference.ChangedBy));
            Assert.That(rev.Fields[0].Value, Is.EqualTo(rev.Author));
        }

        [Test]
        public void When_calling_ensure_assignee_field_with_empty_args_Then_an_exception_is_thrown()
        {
            MockedWitClientWrapper witClientWrapper = new MockedWitClientWrapper();
            WitClientUtils wiUtils = new WitClientUtils(witClientWrapper);
            Assert.That(
                () => wiUtils.EnsureAssigneeField(null, null),
                Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void When_calling_ensure_assignee_field_with_first_revision_Then_assignee_is_added_to_fields()
        {
            MockedWitClientWrapper witClientWrapper = new MockedWitClientWrapper();
            WitClientUtils wiUtils = new WitClientUtils(witClientWrapper);

            WiRevision rev = new WiRevision();
            rev.Fields = new List<WiField>();
            rev.Index = 0;

            WorkItem createdWI = wiUtils.CreateWorkItem("User Story");
            createdWI.Fields[WiFieldReference.AssignedTo] = "Mr. Test";

            wiUtils.EnsureAssigneeField(rev, createdWI);

            Assert.That(rev.Fields[0].ReferenceName, Is.EqualTo(WiFieldReference.AssignedTo));
            Assert.That(rev.Fields[0].Value, Is.EqualTo(createdWI.Fields[WiFieldReference.AssignedTo]));
        }

        [Test]
        public void When_calling_ensure_date_fields_with_empty_args_Then_an_exception_is_thrown()
        {
            MockedWitClientWrapper witClientWrapper = new MockedWitClientWrapper();
            WitClientUtils wiUtils = new WitClientUtils(witClientWrapper);
            Assert.That(
                () => wiUtils.EnsureDateFields(null, null),
                Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void When_calling_ensure_date_fields_with_first_revision_Then_dates_are_added_to_fields()
        {
            MockedWitClientWrapper witClientWrapper = new MockedWitClientWrapper();
            WitClientUtils wiUtils = new WitClientUtils(witClientWrapper);

            WiRevision rev = new WiRevision();
            rev.Fields = new List<WiField>();
            rev.Index = 0;

            WorkItem createdWI = wiUtils.CreateWorkItem("User Story");
            createdWI.Fields[WiFieldReference.ChangedDate] = DateTime.Now;

            wiUtils.EnsureDateFields(rev, createdWI);

            Assert.That(rev.Fields[0].ReferenceName, Is.EqualTo(WiFieldReference.CreatedDate));
            Assert.That(
                DateTime.Parse(rev.Fields[0].Value.ToString()),
                Is.LessThan(createdWI.Fields[WiFieldReference.ChangedDate]));

            Assert.That(rev.Fields[1].ReferenceName, Is.EqualTo(WiFieldReference.ChangedDate));
            Assert.That(
                DateTime.Parse(rev.Fields[1].Value.ToString()),
                Is.EqualTo(DateTime.Parse(rev.Fields[0].Value.ToString())));
            //Assert.That(rev.Fields[0].Value, Is.EqualTo(createdWI.Fields[WiFieldReference.AssignedTo]));
        }

        [Test]
        public void When_calling_ensure_fields_on_state_change_with_empty_args_Then_an_exception_is_thrown()
        {
            MockedWitClientWrapper witClientWrapper = new MockedWitClientWrapper();
            WitClientUtils wiUtils = new WitClientUtils(witClientWrapper);
            Assert.That(
                () => wiUtils.EnsureFieldsOnStateChange(null, null),
                Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void When_calling_ensure_fields_on_state_change_with_subsequent_revision_Then_dates_are_added_to_fields()
        {
            MockedWitClientWrapper witClientWrapper = new MockedWitClientWrapper();
            WitClientUtils wiUtils = new WitClientUtils(witClientWrapper);

            WiRevision rev = new WiRevision();
            rev.Fields = new List<WiField>();
            rev.Index = 1;

            WiField revState = new WiField();
            revState.ReferenceName = WiFieldReference.State;
            revState.Value = "New";
            rev.Fields.Add(revState);

            WorkItem createdWI = wiUtils.CreateWorkItem("User Story");
            createdWI.Fields[WiFieldReference.State] = "Done";
            createdWI.Fields[WiFieldReference.ChangedDate] = DateTime.Now;

            wiUtils.EnsureFieldsOnStateChange(rev, createdWI);

            Assert.That(rev.Fields.GetFieldValueOrDefault<string>(WiFieldReference.State), Is.EqualTo("New"));
            Assert.That(rev.Fields.GetFieldValueOrDefault<string>(WiFieldReference.ClosedDate), Is.EqualTo(null));
            Assert.That(rev.Fields.GetFieldValueOrDefault<string>(WiFieldReference.ClosedBy), Is.EqualTo(null));
            Assert.That(rev.Fields.GetFieldValueOrDefault<string>(WiFieldReference.ActivatedDate), Is.EqualTo(null));
            Assert.That(rev.Fields.GetFieldValueOrDefault<string>(WiFieldReference.ActivatedBy), Is.EqualTo(null));
        }

        [Test]
        public void When_calling_ensure_classification_fields_with_empty_args_Then_an_exception_is_thrown()
        {
            MockedWitClientWrapper witClientWrapper = new MockedWitClientWrapper();
            WitClientUtils wiUtils = new WitClientUtils(witClientWrapper);
            Assert.That(
                () => wiUtils.EnsureClassificationFields(null),
                Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void When_calling_ensure_classification_fields_Then_areapath_and_iterationpath_are_added_to_fields()
        {
            WiRevision rev = new WiRevision();
            rev.Fields = new List<WiField>();

            MockedWitClientWrapper witClientWrapper = new MockedWitClientWrapper();
            WitClientUtils wiUtils = new WitClientUtils(witClientWrapper);
            wiUtils.EnsureClassificationFields(rev);

            List<WiField> filteredForAreaPath = rev.Fields.FindAll(f => f.ReferenceName == WiFieldReference.AreaPath && f.Value.ToString() == "");
            List<WiField> filteredForIterationPath = rev.Fields.FindAll(f => f.ReferenceName == WiFieldReference.IterationPath && f.Value.ToString() == "");

            Assert.That(filteredForAreaPath.Count, Is.EqualTo(1));
            Assert.That(filteredForIterationPath.Count, Is.EqualTo(1));
        }

        [Test]
        public void When_calling_ensure_workitem_fields_initialized_with_empty_args_Then_an_exception_is_thrown()
        {
            MockedWitClientWrapper witClientWrapper = new MockedWitClientWrapper();
            WitClientUtils wiUtils = new WitClientUtils(witClientWrapper);
            Assert.That(
                () => wiUtils.EnsureWorkItemFieldsInitialized(null, null),
                Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void When_calling_ensure_workitem_fields_initialized_for_user_story_Then_title_and_description_are_added_to_fields()
        {
            MockedWitClientWrapper witClientWrapper = new MockedWitClientWrapper();
            WitClientUtils wiUtils = new WitClientUtils(witClientWrapper);

            WorkItem createdWI = wiUtils.CreateWorkItem("User Story");

            WiRevision rev = new WiRevision();
            rev.Fields = new List<WiField>();

            WiField revTitleField = new WiField();
            revTitleField.ReferenceName = WiFieldReference.Title;
            revTitleField.Value = "My title";

            rev.Fields.Add(revTitleField);

            wiUtils.EnsureWorkItemFieldsInitialized(rev, createdWI);

            Assert.That(createdWI.Fields[WiFieldReference.Title],
                Is.EqualTo(rev.Fields.GetFieldValueOrDefault<string>(WiFieldReference.Title)));
            Assert.That(createdWI.Fields[WiFieldReference.Description],
                Is.EqualTo(""));
        }

        [Test]
        public void When_calling_ensure_workitem_fields_initialized_for_bug_Then_title_and_description_are_added_to_fields()
        {
            MockedWitClientWrapper witClientWrapper = new MockedWitClientWrapper();
            WitClientUtils wiUtils = new WitClientUtils(witClientWrapper);

            WorkItem createdWI = wiUtils.CreateWorkItem("Bug");

            WiRevision rev = new WiRevision();
            rev.Fields = new List<WiField>();

            WiField revTitleField = new WiField();
            revTitleField.ReferenceName = WiFieldReference.Title;
            revTitleField.Value = "My title";

            rev.Fields.Add(revTitleField);

            wiUtils.EnsureWorkItemFieldsInitialized(rev, createdWI);

            Assert.That(createdWI.Fields[WiFieldReference.Title],
                Is.EqualTo(rev.Fields.GetFieldValueOrDefault<string>(WiFieldReference.Title)));
            Assert.That(createdWI.Fields[WiFieldReference.ReproSteps],
                Is.EqualTo(""));
        }

        [Test]
        public void When_calling_is_duplicate_work_item_link_with_empty_args_Then_an_exception_is_thrown()
        {
            MockedWitClientWrapper witClientWrapper = new MockedWitClientWrapper();
            WitClientUtils wiUtils = new WitClientUtils(witClientWrapper);
            Assert.That(
                () => wiUtils.IsDuplicateWorkItemLink(null, null),
                Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void When_calling_is_duplicate_work_item_link_with_no_containing_links_Then_false_is_returned()
        {
            WorkItemRelation[] links = new WorkItemRelation[0];
            WorkItemRelation relatedLink = new WorkItemRelation();

            MockedWitClientWrapper witClientWrapper = new MockedWitClientWrapper();
            WitClientUtils wiUtils = new WitClientUtils(witClientWrapper);
            bool isDuplicate = wiUtils.IsDuplicateWorkItemLink(links, relatedLink);

            Assert.That(isDuplicate, Is.EqualTo(false));
        }

        [Test]
        public void When_calling_create_work_item_Then_work_item_is_created_and_added_to_cache()
        {
            MockedWitClientWrapper witClientWrapper = new MockedWitClientWrapper();
            WitClientUtils wiUtils = new WitClientUtils(witClientWrapper);

            WorkItem createdWI = wiUtils.CreateWorkItem("Task");
            WorkItem retrievedWI = null;
            if (createdWI.Id.HasValue)
            {
                retrievedWI = wiUtils.GetWorkItem(createdWI.Id.Value);
            }

            Assert.That(createdWI.Id, Is.EqualTo(1));
            Assert.That(retrievedWI.Id, Is.EqualTo(1));

            Assert.That(createdWI.Fields[WiFieldReference.WorkItemType], Is.EqualTo("Task"));
            Assert.That(retrievedWI.Fields[WiFieldReference.WorkItemType], Is.EqualTo("Task"));
        }

        [Test]
        public void When_calling_set_field_value_with_empty_args_Then_an_exception_is_thrown()
        {
            MockedWitClientWrapper witClientWrapper = new MockedWitClientWrapper();
            WitClientUtils wiUtils = new WitClientUtils(witClientWrapper);

            Assert.That(
                () => wiUtils.SetFieldValue(null, "", ""),
                Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void When_calling_set_field_value_with_valid_args_Then_a_field_is_added()
        {
            string iterationPathValue = "My/Iteration/Path";

            MockedWitClientWrapper witClientWrapper = new MockedWitClientWrapper();
            WitClientUtils wiUtils = new WitClientUtils(witClientWrapper);

            WorkItem createdWI = wiUtils.CreateWorkItem("Task");

            wiUtils.SetFieldValue(createdWI, WiFieldReference.IterationPath, iterationPathValue);

            Assert.That(createdWI.Fields[WiFieldReference.IterationPath], Is.EqualTo(iterationPathValue));
        }

        [Test]
        public void When_calling_add_link_with_empty_args_Then_an_exception_is_thrown()
        {
            MockedWitClientWrapper witClientWrapper = new MockedWitClientWrapper();
            WitClientUtils wiUtils = new WitClientUtils(witClientWrapper);

            Assert.That(
                () => wiUtils.AddLink(null, null),
                Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void When_calling_add_link_with_valid_args_Then_a_link_is_added()
        {
            MockedWitClientWrapper witClientWrapper = new MockedWitClientWrapper();
            WitClientUtils wiUtils = new WitClientUtils(witClientWrapper);

            WorkItem createdWI = wiUtils.CreateWorkItem("User Story");
            WorkItem linkedWI = wiUtils.CreateWorkItem("Task");

            WiLink link = new WiLink();
            link.WiType = "System.LinkTypes.Hierarchy-Forward";
            link.SourceOriginId = "100";
            link.SourceWiId = 1;
            link.TargetOriginId = "101";
            link.TargetWiId = 2;
            link.Change = ReferenceChangeType.Added;

            wiUtils.AddLink(link, createdWI);

            WorkItemRelation rel = createdWI.Relations[0];

            Assert.That(rel.Rel, Is.EqualTo(link.WiType));
            Assert.That(rel.Url, Is.EqualTo(linkedWI.Url));
        }

        [Test]
        public void When_calling_remove_link_with_empty_args_Then_an_exception_is_thrown()
        {
            MockedWitClientWrapper witClientWrapper = new MockedWitClientWrapper();
            WitClientUtils wiUtils = new WitClientUtils(witClientWrapper);

            Assert.That(
                () => wiUtils.RemoveLink(null, null),
                Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void When_calling_remove_link_with_no_link_added_Then_false_is_returned()
        {
            MockedWitClientWrapper witClientWrapper = new MockedWitClientWrapper();
            WitClientUtils wiUtils = new WitClientUtils(witClientWrapper);

            WorkItem createdWI = wiUtils.CreateWorkItem("User Story");

            WiLink link = new WiLink();
            link.WiType = "System.LinkTypes.Hierarchy-Forward";

            bool result = wiUtils.RemoveLink(link, createdWI);

            Assert.That(result, Is.EqualTo(false));
        }

        [Test]
        public void When_calling_remove_link_with_link_added_Then_link_is_removed()
        {
            MockedWitClientWrapper witClientWrapper = new MockedWitClientWrapper();
            WitClientUtils wiUtils = new WitClientUtils(witClientWrapper);

            WorkItem createdWI = wiUtils.CreateWorkItem("User Story");
            WorkItem linkedWI = wiUtils.CreateWorkItem("Task");

            WiLink link = new WiLink();
            link.WiType = "System.LinkTypes.Hierarchy-Forward";
            link.SourceOriginId = "100";
            link.SourceWiId = 1;
            link.TargetOriginId = "101";
            link.TargetWiId = 2;
            link.Change = ReferenceChangeType.Added;

            wiUtils.AddLink(link, createdWI);

            bool result = wiUtils.RemoveLink(link, createdWI);

            Assert.That(result, Is.EqualTo(true));
            Assert.That(createdWI.Relations, Is.Empty);
        }

        [Test]
        public void When_calling_correct_comment_with_empty_args_Then_an_exception_is_thrown()
        {
            MockedWitClientWrapper witClientWrapper = new MockedWitClientWrapper();
            WitClientUtils wiUtils = new WitClientUtils(witClientWrapper);

            Assert.That(
                () => wiUtils.CorrectComment(null, null, null, MockedIsAttachmentMigratedDelegateTrue),
                Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void When_calling_correct_comment_with_valid_args_Then_history_is_updated_with_correct_image_urls()
        {
            string commentBeforeTransformation = "My comment, including file: <img src=\"my_image.png\">";
            string commentAfterTransformation = "My comment, including file: <img src=\"https://example.com/my_image.png\">";

            MockedWitClientWrapper witClientWrapper = new MockedWitClientWrapper();
            WitClientUtils wiUtils = new WitClientUtils(witClientWrapper);

            WorkItem createdWI = wiUtils.CreateWorkItem("Task");
            createdWI.Fields[WiFieldReference.History] = commentBeforeTransformation;
            createdWI.Relations.Add(new WorkItemRelation() {
                Rel= "AttachedFile",
                Url= "https://example.com/my_image.png",
                Attributes = new Dictionary<string, object>() { { "filePath", "C:\\Temp\\MyFiles\\my_image.png" } }
            });

            WiAttachment att = new WiAttachment();
            att.Change = ReferenceChangeType.Added;
            att.FilePath = "C:\\Temp\\MyFiles\\my_image.png";

            WiRevision revision = new WiRevision();
            revision.Attachments.Add(att);

            WiItem wiItem = new WiItem();
            wiItem.Revisions = new List<WiRevision>();
            wiItem.Revisions.Add(revision);

            wiUtils.CorrectComment(createdWI, wiItem, revision, MockedIsAttachmentMigratedDelegateTrue);

            Assert.That(createdWI.Fields[WiFieldReference.History], Is.EqualTo(commentAfterTransformation));
        }

        [Test]
        public void When_calling_correct_description_with_empty_args_Then_an_exception_is_thrown()
        {
            MockedWitClientWrapper witClientWrapper = new MockedWitClientWrapper();
            WitClientUtils wiUtils = new WitClientUtils(witClientWrapper);

            Assert.That(
                () => wiUtils.CorrectDescription(null, null, null, MockedIsAttachmentMigratedDelegateTrue),
                Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void When_calling_correct_description_for_user_story_Then_description_is_updated_with_correct_image_urls()
        {
            string descriptionBeforeTransformation = "My description, including file: <img src=\"my_image.png\">";
            string descriptionAfterTransformation = "My description, including file: <img src=\"https://example.com/my_image.png\">";

            MockedWitClientWrapper witClientWrapper = new MockedWitClientWrapper();
            WitClientUtils wiUtils = new WitClientUtils(witClientWrapper);

            WorkItem createdWI = wiUtils.CreateWorkItem("User Story");
            createdWI.Fields[WiFieldReference.Description] = descriptionBeforeTransformation;
            createdWI.Relations.Add(new WorkItemRelation()
            {
                Rel = "AttachedFile",
                Url = "https://example.com/my_image.png",
                Attributes = new Dictionary<string, object>() { { "filePath", "C:\\Temp\\MyFiles\\my_image.png" } }
            });

            WiAttachment att = new WiAttachment();
            att.Change = ReferenceChangeType.Added;
            att.FilePath = "C:\\Temp\\MyFiles\\my_image.png";

            WiRevision revision = new WiRevision();
            revision.Attachments.Add(att);

            WiItem wiItem = new WiItem();
            wiItem.Revisions = new List<WiRevision>();
            wiItem.Revisions.Add(revision);

            wiUtils.CorrectDescription(createdWI, wiItem, revision, MockedIsAttachmentMigratedDelegateTrue);

            Assert.That(createdWI.Fields[WiFieldReference.Description], Is.EqualTo(descriptionAfterTransformation));
        }

        [Test]
        public void When_calling_correct_description_for_bug_Then_repro_steps_is_updated_with_correct_image_urls()
        {
            string reproStepsBeforeTransformation = "My description, including file: <img src=\"my_image.png\">";
            string reproStepsAfterTransformation = "My description, including file: <img src=\"https://example.com/my_image.png\">";

            MockedWitClientWrapper witClientWrapper = new MockedWitClientWrapper();
            WitClientUtils wiUtils = new WitClientUtils(witClientWrapper);

            WorkItem createdWI = wiUtils.CreateWorkItem("Bug");
            createdWI.Fields[WiFieldReference.ReproSteps] = reproStepsBeforeTransformation;
            createdWI.Relations.Add(new WorkItemRelation()
            {
                Rel = "AttachedFile",
                Url = "https://example.com/my_image.png",
                Attributes = new Dictionary<string, object>() { { "filePath", "C:\\Temp\\MyFiles\\my_image.png" } }
            });

            WiAttachment att = new WiAttachment();
            att.Change = ReferenceChangeType.Added;
            att.FilePath = "C:\\Temp\\MyFiles\\my_image.png";

            WiRevision revision = new WiRevision();
            revision.Attachments.Add(att);

            WiItem wiItem = new WiItem();
            wiItem.Revisions = new List<WiRevision>();
            wiItem.Revisions.Add(revision);

            wiUtils.CorrectDescription(createdWI, wiItem, revision, MockedIsAttachmentMigratedDelegateTrue);

            Assert.That(createdWI.Fields[WiFieldReference.ReproSteps], Is.EqualTo(reproStepsAfterTransformation));
        }

        [Test]
        public void When_calling_apply_attachments_with_empty_args_Then_an_exception_is_thrown()
        {
            MockedWitClientWrapper witClientWrapper = new MockedWitClientWrapper();
            WitClientUtils wiUtils = new WitClientUtils(witClientWrapper);

            Assert.That(
                () => wiUtils.ApplyAttachments(null, null, null, MockedIsAttachmentMigratedDelegateTrue),
                Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void When_calling_apply_attachments_with_change_equal_to_added_Then_workitem_is_updated_with_correct_attachment()
        {
            MockedWitClientWrapper witClientWrapper = new MockedWitClientWrapper();
            WitClientUtils wiUtils = new WitClientUtils(witClientWrapper);

            WorkItem createdWI = wiUtils.CreateWorkItem("User Story");

            WiAttachment att = new WiAttachment();
            att.Change = ReferenceChangeType.Added;
            att.FilePath = "C:\\Temp\\MyFiles\\my_image.png";
            att.AttOriginId = "100";
            att.Comment = "My comment";

            WiRevision revision = new WiRevision();
            revision.Attachments.Add(att);

            Dictionary<string, WiAttachment> attachmentMap = new Dictionary<string, WiAttachment>();

            wiUtils.ApplyAttachments(revision, createdWI, attachmentMap, MockedIsAttachmentMigratedDelegateTrue);

            Assert.That(createdWI.Relations[0].Rel, Is.EqualTo("AttachedFile"));
            Assert.That(createdWI.Relations[0].Attributes["filePath"], Is.EqualTo(att.FilePath));
            Assert.That(createdWI.Relations[0].Attributes["comment"], Is.EqualTo(att.Comment));
        }

        [Test]
        public void When_calling_apply_attachments_with_change_equal_to_removed_Then_workitem_is_updated_with_removed_attachment()
        {
            string attachmentFilePath = "C:\\Temp\\MyFiles\\my_image.png";

            MockedWitClientWrapper witClientWrapper = new MockedWitClientWrapper();
            WitClientUtils wiUtils = new WitClientUtils(witClientWrapper);

            WorkItem createdWI = wiUtils.CreateWorkItem("User Story");
            createdWI.Relations.Add(new WorkItemRelation()
            {
                Rel = "AttachedFile",
                Url = "https://example.com/my_image.png",
                Attributes = new Dictionary<string, object>() { { "filePath", attachmentFilePath } }
            });

            WiAttachment att = new WiAttachment();
            att.Change = ReferenceChangeType.Removed;
            att.FilePath = attachmentFilePath;
            att.AttOriginId = "100";
            att.Comment = "My comment";

            WiRevision revision = new WiRevision();
            revision.Attachments.Add(att);

            Dictionary<string, WiAttachment> attachmentMap = new Dictionary<string, WiAttachment>();

            wiUtils.ApplyAttachments(revision, createdWI, attachmentMap, MockedIsAttachmentMigratedDelegateTrue);

            Assert.That(createdWI.Relations, Is.Empty);
        }

        [Test]
        public void When_calling_apply_attachments_with_already_existing_attachment_Then_workitem_is_updated_with_another_attachment()
        {
            string attachmentFilePath = "C:\\Temp\\MyFiles\\my_image.png";

            MockedWitClientWrapper witClientWrapper = new MockedWitClientWrapper();
            WitClientUtils wiUtils = new WitClientUtils(witClientWrapper);

            WorkItem createdWI = wiUtils.CreateWorkItem("User Story");
            createdWI.Relations.Add(new WorkItemRelation()
            {
                Rel = "AttachedFile",
                Url = "https://example.com/my_image.png",
                Attributes = new Dictionary<string, object>() { { "filePath", attachmentFilePath } }
            });

            WiAttachment att = new WiAttachment();
            att.Change = ReferenceChangeType.Added;
            att.FilePath = attachmentFilePath;
            att.AttOriginId = "100";
            att.Comment = "My comment";

            WiRevision revision = new WiRevision();
            revision.Attachments.Add(att);

            Dictionary<string, WiAttachment> attachmentMap = new Dictionary<string, WiAttachment>();

            wiUtils.ApplyAttachments(revision, createdWI, attachmentMap, MockedIsAttachmentMigratedDelegateFalse);

            Assert.That(createdWI.Relations.Count, Is.EqualTo(2));
        }
        //TODO: test SaveWorkItem

        [Test]
        public void When_calling_save_workitem_with_empty_args_Then_an_exception_is_thrown()
        {
            MockedWitClientWrapper witClientWrapper = new MockedWitClientWrapper();
            WitClientUtils wiUtils = new WitClientUtils(witClientWrapper);

            Assert.That(
                () => wiUtils.SaveWorkItem(null, null),
                Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void When_calling_save_workitem_with_populated_workitem_Then_workitem_is_updated_in_store()
        {
            MockedWitClientWrapper witClientWrapper = new MockedWitClientWrapper();
            WitClientUtils wiUtils = new WitClientUtils(witClientWrapper);

            WorkItem createdWI = wiUtils.CreateWorkItem("User Story");
            WorkItem linkedWI = wiUtils.CreateWorkItem("Task");

            // Add fields
            createdWI.Fields[WiFieldReference.Title] = "My work item";
            createdWI.Fields[WiFieldReference.Description] = "My description";
            createdWI.Fields[WiFieldReference.Priority] = "1";

            // Add attachment

            WiAttachment att = new WiAttachment();
            att.Change = ReferenceChangeType.Added;
            att.FilePath = "C:\\Temp\\MyFiles\\my_image.png";
            att.AttOriginId = "100";
            att.Comment = "My comment";

            WiRevision revision = new WiRevision();
            revision.Attachments.Add(att);

            // Add links
            WiLink link = new WiLink();
            link.WiType = "System.LinkTypes.Hierarchy-Forward";
            link.SourceOriginId = "100";
            link.SourceWiId = 1;
            link.TargetOriginId = "101";
            link.TargetWiId = 2;
            link.Change = ReferenceChangeType.Added;

            revision.Links.Add(link);

            // Perform save

            wiUtils.SaveWorkItem(revision, createdWI);

            WorkItem updatedWI = null;

            if (createdWI.Id.HasValue)
            {
                updatedWI = wiUtils.GetWorkItem(createdWI.Id.Value);
            }

            // Assertions

            Assert.That(updatedWI.Fields[WiFieldReference.Title], Is.EqualTo(createdWI.Fields[WiFieldReference.Title]));
            Assert.That(updatedWI.Fields[WiFieldReference.Description], Is.EqualTo(createdWI.Fields[WiFieldReference.Description]));
            Assert.That(updatedWI.Fields[WiFieldReference.Priority], Is.EqualTo(createdWI.Fields[WiFieldReference.Priority]));

            Assert.That(createdWI.Relations[0].Rel, Is.EqualTo("AttachedFile"));
            Assert.That(createdWI.Relations[0].Url, Is.EqualTo("https://example.com"));
            Assert.That(createdWI.Relations[0].Attributes["comment"].ToString().Split('|')[0], Is.EqualTo(att.Comment));
            Assert.That(createdWI.Relations[0].Attributes["comment"].ToString().Split('|')[1], Is.EqualTo(att.FilePath));

            Assert.That(createdWI.Relations[1].Rel, Is.EqualTo(revision.Links[0].WiType));
            Assert.That(createdWI.Relations[1].Url, Is.EqualTo($"https://example/workItems/{revision.Links[0].TargetWiId}"));

        }
    }
}