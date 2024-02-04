using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Migration.Common.Log;
using Migration.WIContract;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using WorkItemImport.WitClient;

namespace WorkItemImport
{
    public class WitClientWrapper : IWitClientWrapper
    {
        // Cache fields
        private readonly ConcurrentDictionary<string, TeamProject> _projectCache = new ConcurrentDictionary<string, TeamProject>();
        private readonly ConcurrentDictionary<string, GitRepository> _repositoryCache = new ConcurrentDictionary<string, GitRepository>();

        private WorkItemTrackingHttpClient WitClient { get; }
        private ProjectHttpClient ProjectClient { get; }
        private VssConnection Connection { get; }
        private TeamProjectReference TeamProject { get; }
        private GitHttpClient GitClient { get; }

        public WitClientWrapper(string collectionUri, string project, string personalAccessToken)
        {
            var credentials = new VssBasicCredential("", personalAccessToken);
            Connection = new VssConnection(new Uri(collectionUri), credentials);
            WitClient = Connection.GetClient<WorkItemTrackingHttpClient>();
            ProjectClient = Connection.GetClient<ProjectHttpClient>();
            TeamProject = ProjectClient.GetProject(project).Result;
            GitClient = Connection.GetClient<GitHttpClient>();
        }

        public WorkItem CreateWorkItem(string wiType, bool suppressNotifications, DateTime? createdDate = null, string createdBy = "")
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
                wiOut = WitClient.CreateWorkItemAsync(document: patchDoc, project: TeamProject.Name, type: wiType, bypassRules: true, suppressNotifications: suppressNotifications, expand: WorkItemExpand.All).Result;
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Error, $"Error when creating new Work item: {e.Message} - {(e.InnerException != null ? e.InnerException.Message : "")}");
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

        public WorkItem UpdateWorkItem(JsonPatchDocument patchDocument, int workItemId, bool suppressNotifications)
        {
            return WitClient.UpdateWorkItemAsync(
                document: patchDocument,
                id: workItemId,
                suppressNotifications: suppressNotifications,
                bypassRules: true,
                expand: WorkItemExpand.All
            ).Result;
        }

        public TeamProject GetProject(string projectId)
        {
            // Check cache first
            if (_projectCache.TryGetValue(projectId, out var cachedProject))
            {
                return cachedProject;
            }

            // If not in cache, fetch and store in cache
            var project = ProjectClient.GetProject(projectId).Result;
            _projectCache[projectId] = project;
            return project;
        }

        public GitRepository GetRepository(string project, string repository)
        {
            string cacheKey = $"{project}-{repository}";

            // Check cache first
            if (_repositoryCache.TryGetValue(cacheKey, out var cachedRepository))
            {
                return cachedRepository;
            }

            // If not in cache, fetch and store in cache
            var repo = GitClient.GetRepositoryAsync(project, repository).Result;
            _repositoryCache[cacheKey] = repo;
            return repo;
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
