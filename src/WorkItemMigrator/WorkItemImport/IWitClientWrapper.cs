using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

namespace WorkItemImport
{
    public interface IWitClientWrapper
    {
        Task<WorkItem> CreateWorkItem(string wiType);
        Task<WorkItem> GetWorkItem(int wiId);
        Task<WorkItem> UpdateWorkItem(JsonPatchDocument patchDocument, int workItemId);
        Task<TeamProject> GetProject(string projectId);
        Task<List<WorkItemRelationType>> GetRelationTypes();
    }
}
