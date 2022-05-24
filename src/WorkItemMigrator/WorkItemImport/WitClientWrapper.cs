using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

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
            TeamProject = ProjectClient.GetProject(project).Result;
            WorkItemsAdded = new HashSet<int>();
            DefaultWorkItemTypeCategory = WitClient.GetWorkItemTypeCategoryAsync(TeamProject.Id, DefaultCategoryReferenceName).Result;
            DefaultWorkItemType = DefaultWorkItemTypeCategory.DefaultWorkItemType;
        }

        public WorkItem CreateWorkItem(string wiType)
        {
            WorkItem a = null;
            try
            {
                a = WitClient.CreateWorkItemAsync(new JsonPatchDocument(), TeamProject.Name, wiType).Result;
            } catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            
            return a;
        }

        public WorkItem GetWorkItem(int wiId)
        {
            return WitClient.GetWorkItemAsync(wiId).Result;
        }

        public WorkItem UpdateWorkItem(JsonPatchDocument patchDocument, int workItemId)
        {
            return WitClient.UpdateWorkItemAsync(patchDocument, workItemId).Result;
        }

        public TeamProject GetProject(string projectId)
        {
            return ProjectClient.GetProject(projectId).Result;
        }

        public List<WorkItemRelationType> GetRelationTypes()
        {
            return WitClient.GetRelationTypesAsync().Result;
        }
    }
}