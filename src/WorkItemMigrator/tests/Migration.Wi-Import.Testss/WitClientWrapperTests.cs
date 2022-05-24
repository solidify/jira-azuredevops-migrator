using NUnit.Framework;

using AutoFixture.AutoNSubstitute;
using AutoFixture;
using System;
using WorkItemImport;
using Migration.WIContract;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using System.Linq;

namespace Migration.Wi_Import.Testss
{
    [TestFixture]
    public class WitClientWrapperTests
    {
        private class MockedWitClientWrapper : IWitClientWrapper
        {
            private int _wiIdCounter = 1;
            private Dictionary<int, WorkItem> _wiCache = new Dictionary<int, WorkItem>();

            public MockedWitClientWrapper()
            {

            }

            public WorkItem CreateWorkItem(string wiType)
            {
                WorkItem workItem = new WorkItem();
                workItem.Id = _wiIdCounter;
                workItem.Fields[WiFieldReference.WorkItemType] = wiType;
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
                            wi.Relations.Add(op.Value as WorkItemRelation);
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
                WorkItemRelationType issue = new WorkItemRelationType();
                issue.ReferenceName = "Issue";
                WorkItemRelationType task = new WorkItemRelationType();
                task.ReferenceName = "Task";
                WorkItemRelationType userStory = new WorkItemRelationType();
                userStory.ReferenceName = "User Story";
                WorkItemRelationType bug = new WorkItemRelationType();
                bug.ReferenceName = "Bug";

                List<WorkItemRelationType> outList = new List<WorkItemRelationType>();
                outList.Add(issue);
                outList.Add(task);
                outList.Add(userStory);
                outList.Add(bug);
                return outList;
            }
        }
        private bool MockedIsAttachmentMigratedDelegate(string _attOriginId, out string attWiId)
        {
            attWiId = "1";
            return true;
        }

        // use auto fixiture to help mock and instantiate with dummy data with nsubsitute. 
        private Fixture _fixture;

        [SetUp]
        public void Setup()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoNSubstituteCustomization() { });
        }

        [Test]
        public void When_calling_correct_image_path_with_null_args_Then_an_exception_is_thrown()
        {
            MockedWitClientWrapper witClientWrapper = new MockedWitClientWrapper();
            WitClientUtils wiUtils = new WitClientUtils(witClientWrapper);

            string textField = "";
            bool isUpdated = true;

            Assert.That(
                () => wiUtils.CorrectImagePath(null, null, null, ref textField, ref isUpdated, MockedIsAttachmentMigratedDelegate),
                Throws.InstanceOf<ArgumentException>());
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
    }
}