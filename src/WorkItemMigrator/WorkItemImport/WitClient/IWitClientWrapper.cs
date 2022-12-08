using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Migration.WIContract;

namespace WorkItemImport
{
    public interface IWitClientWrapper
    {
        WorkItem CreateWorkItem(string wiType, DateTime createdDate = default, string createdBy = "");
        WorkItem GetWorkItem(int wiId);
        WorkItem UpdateWorkItem(JsonPatchDocument patchDocument, int workItemId);
        TeamProject GetProject(string projectId);
        List<WorkItemRelationType> GetRelationTypes();
        AttachmentReference CreateAttachment(WiAttachment attachment);
    }
}
