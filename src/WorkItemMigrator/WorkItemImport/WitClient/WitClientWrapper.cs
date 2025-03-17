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
        private int ChangedDateBumpMS { get; }

        public WitClientWrapper(string collectionUri, string project, string personalAccessToken, int changedDateBumpMS)
        {
            var credentials = new VssBasicCredential("", personalAccessToken);
            Connection = new VssConnection(new Uri(collectionUri), credentials);
            WitClient = Connection.GetClient<WorkItemTrackingHttpClient>();
            ProjectClient = Connection.GetClient<ProjectHttpClient>();
            TeamProject = ProjectClient.GetProject(project).Result;
            GitClient = Connection.GetClient<GitHttpClient>();
            ChangedDateBumpMS = changedDateBumpMS;
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
            while (true)
            {
                try
                {
                    var result = WitClient.UpdateWorkItemAsync(
                        document: patchDocument,
                        id: workItemId,
                        suppressNotifications: suppressNotifications,
                        bypassRules: true,
                        expand: WorkItemExpand.All
                    ).Result;
                    return result;
                }
                catch (AggregateException ex)
                {
                    bool datesMustIncreaseError = false;
                    foreach (Exception ex2 in ex.InnerExceptions)
                    {
                        // Handle 'VS402625' error responses, the supplied ChangedDate was older than the latest revision already in ADO.
                        // We must bump the ChangedDate by a small factor and try again.
                        if (ex2.Message.Contains("VS402625"))
                        {
                            foreach (var patchOp in patchDocument)
                            {
                                if (patchOp.Path == "/fields/System.ChangedDate")
                                {
                                    patchOp.Value = ((DateTime)patchOp.Value).AddMilliseconds(ChangedDateBumpMS);
                                    Logger.Log(LogLevel.Warning, $"Received response while updating Work Item: {ex2.Message}." +
                                        $" Bumped ChangedDate by {ChangedDateBumpMS}ms and trying again... New ChangedDate: {patchOp.Value}, ms: " +
                                        ((DateTime)patchOp.Value).Millisecond);
                                    break;
                                }
                            }
                            datesMustIncreaseError = true;
                        }
                    }
                    if (!datesMustIncreaseError)
                    {
                        throw;
                    }
                }
            }
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
