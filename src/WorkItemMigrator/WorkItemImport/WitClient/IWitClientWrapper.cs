using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Migration.WIContract;
using System;
using System.Collections.Generic;

namespace WorkItemImport
{
    public interface IWitClientWrapper
    {
        WorkItem CreateWorkItem(string wiType, bool suppressNotifications, DateTime? createdDate = null, string createdBy = "");
        WorkItem GetWorkItem(int wiId);
        WorkItem UpdateWorkItem(JsonPatchDocument patchDocument, int workItemId, bool suppressNotifications);
        TeamProject GetProject(string projectId);
        GitRepository GetRepository(string project, string repository);
        List<WorkItemRelationType> GetRelationTypes();
        AttachmentReference CreateAttachment(WiAttachment attachment);
    }
}
