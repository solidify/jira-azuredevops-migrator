using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
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

        public WitClientWrapper(string collectionUri, string project, string personalAccessToken)
        {
            var credentials = new VssBasicCredential("", personalAccessToken);
            Connection = new VssConnection(new Uri(collectionUri), credentials);
            WitClient = Connection.GetClient<WorkItemTrackingHttpClient>();
            ProjectClient = Connection.GetClient<ProjectHttpClient>();
            TeamProject = ProjectClient.GetProject(project).Result;
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

            WorkItem wiOut;
            try
            {
                wiOut = WitClient.CreateWorkItemAsync(document:patchDoc, project:TeamProject.Name, type:wiType, bypassRules:false, expand:WorkItemExpand.All).Result;
            } catch (Exception e)
            {
                Logger.Log(LogLevel.Error, "Error when creating new Work item: " + e.Message);
                return null;
            }

            if (wiOut.Relations == null)
                wiOut.Relations = new List<WorkItemRelation>();

            return wiOut;
        }

        public WorkItem GetWorkItem(int wiId)
        {
            WorkItem wiOut;
            try
            {
                wiOut = WitClient.GetWorkItemAsync(wiId, expand: WorkItemExpand.All).Result;
            } catch (System.AggregateException)
            {
                // Work item was not found, return null
                return null;
            }
            if (wiOut.Relations == null)
                wiOut.Relations = new List<WorkItemRelation>();
            return wiOut;
        }

        public WorkItem UpdateWorkItem(JsonPatchDocument patchDocument, int workItemId)
        {
            return WitClient.UpdateWorkItemAsync(document:patchDocument, id:workItemId, bypassRules:true, expand: WorkItemExpand.All).Result;
        }

        public TeamProject GetProject(string projectId)
        {
            return ProjectClient.GetProject(projectId).Result;
        }

        public List<WorkItemRelationType> GetRelationTypes()
        {
            return WitClient.GetRelationTypesAsync().Result;
        }

        public AttachmentReference CreateAttachment(WiAttachment attachment)
        {
            using (FileStream uploadStream = File.Open(attachment.FilePath, FileMode.Open, FileAccess.Read))
                return WitClient.CreateAttachmentAsync(uploadStream, attachment.FileName, null, null, null, new CancellationToken()).Result;
        }
    }
}
