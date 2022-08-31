﻿using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
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

        public WitClientWrapper(string collectionUri, string project, string personalAccessToken)
        {
            var credentials = new VssBasicCredential("", personalAccessToken);
            Connection = new VssConnection(new Uri(collectionUri), credentials);
            WitClient = Connection.GetClient<WorkItemTrackingHttpClient>();
            ProjectClient = Connection.GetClient<ProjectHttpClient>();
            TeamProject = ProjectClient.GetProject(project).Result;
            WorkItemsAdded = new HashSet<int>();
            DefaultWorkItemTypeCategory = WitClient.GetWorkItemTypeCategoryAsync(TeamProject.Id, DefaultCategoryReferenceName).Result;
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
                wiOut = WitClient.CreateWorkItemAsync(patchDoc, TeamProject.Name, wiType).Result;
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
                wiOut = WitClient.GetWorkItemAsync(wiId).Result;
            } catch (System.AggregateException)
            {
                // Work item was not found, return null
                return null;
            }
            return wiOut;
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

        public AttachmentReference CreateAttachment(string filePath)
        {
            return WitClient.CreateAttachmentAsync(filePath).Result;
        }
    }
}
