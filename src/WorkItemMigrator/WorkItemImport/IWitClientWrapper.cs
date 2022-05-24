using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

namespace WorkItemImport
{
    public interface IWitClientWrapper
    {
        WorkItem CreateWorkItem(string wiType);
        WorkItem GetWorkItem(int wiId);
        WorkItem UpdateWorkItem(JsonPatchDocument patchDocument, int workItemId);
        TeamProject GetProject(string projectId);
        List<WorkItemRelationType> GetRelationTypes();
    }
}
