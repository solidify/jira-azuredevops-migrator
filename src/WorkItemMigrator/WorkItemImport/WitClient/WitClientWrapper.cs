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
using WorkItemImport.WitClient;

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

        public WorkItem CreateWorkItem(string wiType, DateTime? createdDate = null, string createdBy = "")
        {
            JsonPatchDocument patchDoc = new JsonPatchDocument
            {
                JsonPatchDocUtils.CreateJsonFieldPatchOp(Operation.Add, WiFieldReference.Title, "[Placeholder Name]")
            };

            if (createdDate != null)
            {
                patchDoc.Add(
                    JsonPatchDocUtils.CreateJsonFieldPatchOp(Operation.Add, WiFieldReference.CreatedDate, createdDate)
                );

                patchDoc.Add(
                    JsonPatchDocUtils.CreateJsonFieldPatchOp(Operation.Add, WiFieldReference.ChangedDate, createdDate)
                );
            }

            if (!string.IsNullOrEmpty(createdBy))
            {
                patchDoc.Add(
                    JsonPatchDocUtils.CreateJsonFieldPatchOp(Operation.Add, WiFieldReference.CreatedBy, createdBy)
                );

                patchDoc.Add(
                    JsonPatchDocUtils.CreateJsonFieldPatchOp(Operation.Add, WiFieldReference.ChangedBy, createdBy)
                );
            }

            WorkItem wiOut;
            try
            {
                wiOut = WitClient.CreateWorkItemAsync(document: patchDoc, project: TeamProject.Name, type: wiType, bypassRules: true, expand: WorkItemExpand.All).Result;
            }
            catch (Exception e)
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
            }
            catch (System.AggregateException)
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
            return WitClient.UpdateWorkItemAsync(document: patchDocument, id: workItemId, bypassRules: true, expand: WorkItemExpand.All).Result;
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
