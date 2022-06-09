using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Migration.Common.Log;
using Migration.WIContract;

namespace WorkItemImport
{
    public class WitClientWrapper : IWitClientWrapper
    {
        private WorkItemTrackingHttpClient WitClient { get; }
        private ProjectHttpClient ProjectClient { get; }
        private VssConnection Connection { get; }
        private TeamProjectReference TeamProject { get; }
        private HashSet<int> WorkItemsAdded { get; }
        private readonly string DefaultCategoryReferenceName = "Microsoft.RequirementCategory";
        private WorkItemTypeCategory DefaultWorkItemTypeCategory { get; }
        private WorkItemTypeReference DefaultWorkItemType { get; }

        public WitClientWrapper(string collectionUri, string project)
        {
            Connection = new VssConnection(new Uri(collectionUri), new VssClientCredentials());
            WitClient = Connection.GetClient<WorkItemTrackingHttpClient>();
            ProjectClient = Connection.GetClient<ProjectHttpClient>();
            TeamProject = ProjectClient.GetProject(project).GetAwaiter().GetResult();
            WorkItemsAdded = new HashSet<int>();
            DefaultWorkItemTypeCategory = WitClient.GetWorkItemTypeCategoryAsync(TeamProject.Id, DefaultCategoryReferenceName).GetAwaiter().GetResult();
            DefaultWorkItemType = DefaultWorkItemTypeCategory.DefaultWorkItemType;
        }

        public WorkItem CreateWorkItem(string wiType)
        {
            JsonPatchDocument patchDoc = new JsonPatchDocument
            {
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/"+WiFieldReference.Title,
                    Value = "[Placeholder Name]"
                }
            };

            WorkItem wiOut = null;
            try
            {
                wiOut = WitClient.CreateWorkItemAsync(patchDoc, TeamProject.Name, wiType).GetAwaiter().GetResult();
            } catch (Exception e)
            {
                Logger.Log(LogLevel.Error, "Error when creating new Work item: " + e.Message);
            }
            
            return wiOut;
        }

        public WorkItem GetWorkItem(int wiId)
        {
            WorkItem wiOut;
            try
            {
                wiOut = WitClient.GetWorkItemAsync(wiId).GetAwaiter().GetResult();
            } catch (System.AggregateException)
            {
                // Work item was not found, return null
                return null;
            }
            return wiOut;
        }

        public WorkItem UpdateWorkItem(JsonPatchDocument patchDocument, int workItemId)
        {
            return WitClient.UpdateWorkItemAsync(patchDocument, workItemId).GetAwaiter().GetResult();
        }

        public TeamProject GetProject(string projectId)
        {
            return ProjectClient.GetProject(projectId).GetAwaiter().GetResult();
        }

        public List<WorkItemRelationType> GetRelationTypes()
        {
            return WitClient.GetRelationTypesAsync().GetAwaiter().GetResult();
        }

        public AttachmentReference CreateAttachment(string filePath)
        {
            return WitClient.CreateAttachmentAsync(filePath).GetAwaiter().GetResult();
        }

        public WorkItemClassificationNode CreateOrUpdateClassificationNode(WorkItemClassificationNode postedNode, string project, TreeStructureGroup structureGroup, string path)
        {
            return WitClient.CreateOrUpdateClassificationNodeAsync(postedNode, Guid.Parse(project), structureGroup, path).GetAwaiter().GetResult();
        }
    }
}
