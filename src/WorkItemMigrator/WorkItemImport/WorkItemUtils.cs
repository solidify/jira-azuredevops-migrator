using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
//using Microsoft.TeamFoundation.WorkItemTracking.Client;

using Migration.Common;
using Migration.Common.Log;
using Migration.WIContract;

namespace WorkItemImport
{
    public class WorkItemUtils
    {
        /*
         * Wrapper classes for dotnet core 3.1
         *
        public class WorkItemWrapper
        {
            Dictionary<string, object> Fields { get; set; }
            Dictionary<string, object> Links { get; set; }
            string WIType { get; set; }
            public WorkItem ToWorkItem()
            {
                Microsoft.TeamFoundation.WorkItemTracking.Client.WorkItemType wiType = null;
                switch (WIType)
                {
                    case "Bug":
                        wiType = new Microsoft.TeamFoundation.WorkItemTracking.Client.WorkItemType();
                        break;
                }
                WorkItem wi = new WorkItem(wiType);
                wi.
                return wi;
            }
        }
        */

        private WorkItemTrackingHttpClient WitClient { get; }
        private ProjectHttpClient ProjectClient { get; }
        private VssConnection Connection { get; }
        private TeamProjectReference TeamProject { get; }
        private HashSet<int> WorkItemsAdded { get; }
        private readonly string DefaultCategoryReferenceName = "Microsoft.RequirementCategory";
        private WorkItemTypeCategory DefaultWorkItemTypeCategory { get; }
        private WorkItemTypeReference DefaultWorkItemType { get; }

        public WorkItemUtils(string collectionUri, string project)
        {
            Connection = new VssConnection(new Uri(collectionUri), new VssClientCredentials());
            WitClient = Connection.GetClient<WorkItemTrackingHttpClient>();
            ProjectClient = Connection.GetClient<ProjectHttpClient>();
            TeamProject = ProjectClient.GetProject(project).Result;
            WorkItemsAdded = new HashSet<int>();
            DefaultWorkItemTypeCategory = WitClient.GetWorkItemTypeCategoryAsync(TeamProject.Id, DefaultCategoryReferenceName).Result;
            DefaultWorkItemType = DefaultWorkItemTypeCategory.DefaultWorkItemType;
        }

        public delegate V IsAttachmentMigratedDelegate<T, U, V>(T input, out U output);

        public async Task<WorkItem> CreateWI(string type)
        {
            return await WitClient.CreateWorkItemAsync(null, TeamProject.Id, type);
        }

        public async Task<WorkItem> GetWorkItem(int wiId)
        {
            return await WitClient.GetWorkItemAsync(wiId);
        }

        public void SetFieldValue(WorkItem wi, string fieldRef, object fieldValue)
        {
            try
            {
                JsonPatchDocument patchDocument = new JsonPatchDocument
                {
                    new JsonPatchOperation()
                    {
                        Operation = Operation.Add,
                        Path = "/fields/"+fieldRef,
                        Value = fieldValue.ToString()
                    }
                };

                var result = WitClient.UpdateWorkItemAsync(patchDocument, wi.Id.Value).Result;
            }
            catch (AggregateException ex)
            {
                Logger.Log(LogLevel.Error, ex.InnerException.Message);
            }
        }

        public bool IsDuplicateWorkItemLink(ReferenceLinks links, WorkItemRelation relatedLink)
        {
            if (links == null)
            {
                throw new ArgumentException(nameof(links));
            }

            if (relatedLink == null)
            {
                throw new ArgumentException(nameof(relatedLink));
            }

            var containsRelatedLink = links.Links.Values.Contains(relatedLink);
            var hasSameRelatedWorkItemId = links.Links.Values.OfType<ReferenceLink>()
                .Any(l => l.Href == relatedLink.Href);

            if (!containsRelatedLink && !hasSameRelatedWorkItemId)
                return false;

            Logger.Log(LogLevel.Warning, $"Duplicate work item link detected to related workitem id: {relatedLink.Href}, Skipping link");
            return true;
        }

        public async Task<WorkItemRelationType> ParseLinkEnd(WiLink link, WorkItem wi)
        {
            var props = link.WiType?.Split('-');
            var linkType = (await WitClient.GetRelationTypesAsync()).SingleOrDefault(lt => lt.ReferenceName == props?[0]);
            
            if (linkType == null)
            {
                Logger.Log(LogLevel.Error, $"'{link.ToString()}' - link type ({props?[0]}) does not exist in project");
                return null;
            }

            WorkItemRelationType linkEnd = null;

            if (linkType.IsDirectional)
            {
                if (props?.Length > 1)
                    linkEnd = props[1] == "Forward" ? linkType.ForwardEnd : linkType.ReverseEnd;
                else
                    Logger.Log(LogLevel.Error, $"'{link.ToString()}' - link direction not provided for '{wi.Id}'.");
            }
            else
                linkEnd = linkType.ForwardEnd;

            return linkEnd;
        }

        private bool IsLinkDirectional

        private TeamProject GetProjectFromWorkItem(WorkItem wi)
        {
            string projectName = wi.Fields[WiFieldReference.TeamProject].ToString();
            return ProjectClient.GetProject(projectName).Result;
        }

        public int GetRelatedWorkItemIdFromLink(WorkItemRelation link)
        {
            return int.Parse(link.Url.Split('/')[link.Url.Split('/').Length - 1]);
        }

        public bool RemoveLink(WiLink link, WorkItem wi)
        {
            WorkItemRelation linkToRemove = wi.Links.Links.OfType<WorkItemRelation>().SingleOrDefault(
                rl =>
                    rl.GetType().FullName == link.WiType
                    && GetRelatedWorkItemIdFromLink(rl) == link.TargetWiId);
            if (linkToRemove == null)
            {
                Logger.Log(LogLevel.Warning, $"{link.ToString()} - cannot identify link to remove for '{wi.Id}'.");
                return false;
            }
            wi.Relations.Remove(linkToRemove);
            return true;
        }

        public void EnsureAuthorFields(WiRevision rev)
        {
            if(rev == null)
            {
                throw new ArgumentException(nameof(rev));
            }

            if (rev.Fields == null)
            {
                throw new ArgumentException(nameof(rev.Fields));
            }

            if (rev.Index == 0 && !rev.Fields.HasAnyByRefName(WiFieldReference.CreatedBy))
            {
                rev.Fields.Add(new WiField() { ReferenceName = WiFieldReference.CreatedBy, Value = rev.Author });
            }
            if (!rev.Fields.HasAnyByRefName(WiFieldReference.ChangedBy))
            {
                rev.Fields.Add(new WiField() { ReferenceName = WiFieldReference.ChangedBy, Value = rev.Author });
            }
        }

        public void EnsureAssigneeField(WiRevision rev, WorkItem wi)
        {
            string assignedTo = wi.Fields[WiFieldReference.AssignedTo].ToString();

            if (rev.Fields.HasAnyByRefName(WiFieldReference.AssignedTo))
            {
                var field = rev.Fields.First(f => f.ReferenceName.Equals(WiFieldReference.AssignedTo, StringComparison.InvariantCultureIgnoreCase));
                assignedTo = field.Value?.ToString() ?? string.Empty;
                rev.Fields.RemoveAll(f => f.ReferenceName.Equals(WiFieldReference.AssignedTo, StringComparison.InvariantCultureIgnoreCase));
            }
            rev.Fields.Add(new WiField() { ReferenceName = WiFieldReference.AssignedTo, Value = assignedTo });
        }

        public void EnsureDateFields(WiRevision rev, WorkItem wi)
        {
            if (rev.Index == 0 && !rev.Fields.HasAnyByRefName(WiFieldReference.CreatedDate))
            {
                rev.Fields.Add(new WiField() { ReferenceName = WiFieldReference.CreatedDate, Value = rev.Time.ToString("o") });
            }
            if (!rev.Fields.HasAnyByRefName(WiFieldReference.ChangedDate))
            {
                if (DateTime.Parse(wi.Fields[WiFieldReference.ChangedDate].ToString()) == rev.Time)
                {
                    rev.Fields.Add(new WiField() { ReferenceName = WiFieldReference.ChangedDate, Value = rev.Time.AddMilliseconds(1).ToString("o") });
                }
                else
                    rev.Fields.Add(new WiField() { ReferenceName = WiFieldReference.ChangedDate, Value = rev.Time.ToString("o") });
            }

        }

        public void EnsureFieldsOnStateChange(WiRevision rev, WorkItem wi)
        {
            if (rev.Index != 0 && rev.Fields.HasAnyByRefName(WiFieldReference.State))
            {
                var wiState = wi.Fields[WiFieldReference.State].ToString() ?? string.Empty;
                var revState = rev.Fields.GetFieldValueOrDefault<string>(WiFieldReference.State) ?? string.Empty;
                if (wiState.Equals("Done", StringComparison.InvariantCultureIgnoreCase) && revState.Equals("New", StringComparison.InvariantCultureIgnoreCase))
                {
                    rev.Fields.Add(new WiField() { ReferenceName = WiFieldReference.ClosedDate, Value = null });
                    rev.Fields.Add(new WiField() { ReferenceName = WiFieldReference.ClosedBy, Value = null });

                }
                if (!wiState.Equals("New", StringComparison.InvariantCultureIgnoreCase) && revState.Equals("New", StringComparison.InvariantCultureIgnoreCase))
                {
                    rev.Fields.Add(new WiField() { ReferenceName = WiFieldReference.ActivatedDate, Value = null });
                    rev.Fields.Add(new WiField() { ReferenceName = WiFieldReference.ActivatedBy, Value = null });
                }

                if (revState.Equals("Done", StringComparison.InvariantCultureIgnoreCase) && !rev.Fields.HasAnyByRefName(WiFieldReference.ClosedBy))
                    rev.Fields.Add(new WiField() { ReferenceName = WiFieldReference.ClosedBy, Value = rev.Author });
            }
        }

        public void EnsureClassificationFields(WiRevision rev)
        {
            if (rev == null)
            {
                throw new ArgumentException(nameof(rev));
            }

            if (rev.Fields == null)
            {
                throw new ArgumentException(nameof(rev.Fields));
            }

            if (!rev.Fields.HasAnyByRefName(WiFieldReference.AreaPath))
                rev.Fields.Add(new WiField() { ReferenceName = WiFieldReference.AreaPath, Value = "" });

            if (!rev.Fields.HasAnyByRefName(WiFieldReference.IterationPath))
                rev.Fields.Add(new WiField() { ReferenceName = WiFieldReference.IterationPath, Value = "" });
        }

        public bool ApplyAttachments(WiRevision rev, WorkItem wi, Dictionary<string, AttachmentReference> attachmentMap, IsAttachmentMigratedDelegate<string, int, bool> isAttachmentMigratedDelegate)
        {
            var success = true;

            foreach (var att in rev.Attachments)
            {
                try
                {
                    Logger.Log(LogLevel.Debug, $"Adding attachment '{att.ToString()}'.");
                    if (att.Change == ReferenceChangeType.Added)
                    {
                        var newAttachment = new AttachmentReference(att.FilePath, att.Comment);
                        wi.Attachments.Add(newAttachment);

                        attachmentMap.Add(att.AttOriginId, newAttachment);
                    }
                    else if (att.Change == ReferenceChangeType.Removed)
                    {
                        AttachmentReference existingAttachment = IdentifyAttachment(att, wi, isAttachmentMigratedDelegate);
                        if (existingAttachment != null)
                        {
                            wi.Attachments.Remove(existingAttachment);
                        }
                        else
                        {
                            success = false;
                            Logger.Log(LogLevel.Error, $"Could not find migrated attachment '{att.ToString()}'.");
                        }
                    }
                }
                catch (AbortMigrationException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Logger.Log(ex, $"Failed to apply attachments for '{wi.Id}'.");
                    success = false;
                }
            }

            if (rev.Attachments.Any(a => a.Change == ReferenceChangeType.Removed))
                wi.Fields[WiFieldReference.History] = $"Removed attachments(s): { string.Join(";", rev.Attachments.Where(a => a.Change == ReferenceChangeType.Removed).Select(a => a.ToString()))}";

            return success;
        }

        public AttachmentReference IdentifyAttachment(WiAttachment att, WorkItem wi, IsAttachmentMigratedDelegate<string, int, bool> isAttachmentMigratedDelegate)
        {
            //if (context.Journal.IsAttachmentMigrated(att.AttOriginId, out int attWiId))
            if (isAttachmentMigratedDelegate(att.AttOriginId, out int attWiId))
                return wi.Attachments.Cast<AttachmentReference>().SingleOrDefault(a => a.Id == attWiId);
            return null;
        }

        public void CorrectImagePath(WorkItem wi, WiItem wiItem, WiRevision rev, ref string textField, ref bool isUpdated, IsAttachmentMigratedDelegate<string, int, bool> isAttachmentMigratedDelegate)
        {
            foreach (var att in wiItem.Revisions.SelectMany(r => r.Attachments.Where(a => a.Change == ReferenceChangeType.Added)))
            {
                var fileName = att.FilePath.Split('\\')?.Last() ?? string.Empty;
                if (textField.Contains(fileName))
                {
                    var tfsAtt = IdentifyAttachment(att, wi, isAttachmentMigratedDelegate);

                    if (tfsAtt != null)
                    {
                        string imageSrcPattern = $"src.*?=.*?\"([^\"])(?=.*{att.AttOriginId}).*?\"";
                        textField = Regex.Replace(textField, imageSrcPattern, $"src=\"{tfsAtt.Url}\"");
                        isUpdated = true;
                    }
                    else
                        Logger.Log(LogLevel.Warning, $"Attachment '{att.ToString()}' referenced in text but is missing from work item {wiItem.OriginId}/{wi.Id}.");
                }
            }
            if (isUpdated)
            {
                DateTime changedDate;
                if (wiItem.Revisions.Count > rev.Index + 1)
                    changedDate = RevisionUtility.NextValidDeltaRev(rev.Time, wiItem.Revisions[rev.Index + 1].Time);
                else
                    changedDate = RevisionUtility.NextValidDeltaRev(rev.Time);

                wi.Fields[WiFieldReference.ChangedDate] = changedDate;
                wi.Fields[WiFieldReference.ChangedBy] = rev.Author;
            }
        }

        public bool CorrectDescription(WorkItem wi, WiItem wiItem, WiRevision rev, IsAttachmentMigratedDelegate<string, int, bool> isAttachmentMigratedDelegate)
        {
            string description = wi.Fields[WiFieldReference.WorkItemType].ToString() == "Bug" ? wi.Fields[WiFieldReference.ReproSteps].ToString() : wi.Fields[WiFieldReference.Description].ToString();
            if (string.IsNullOrWhiteSpace(description))
                return false;

            bool descUpdated = false;

            CorrectImagePath(wi, wiItem, rev, ref description, ref descUpdated, isAttachmentMigratedDelegate);

            if (descUpdated)
            {
                if (wi.Fields[WiFieldReference.WorkItemType].ToString() == "Bug")
                {
                    wi.Fields[WiFieldReference.ReproSteps] = description;
                }
                else
                {
                    wi.Fields[WiFieldReference.Description] = description;
                }
            }

            return descUpdated;
        }

        public void CorrectComment(WorkItem wi, WiItem wiItem, WiRevision rev, IsAttachmentMigratedDelegate<string, int, bool> isAttachmentMigratedDelegate)
        {
            string currentComment = wi.Fields[WiFieldReference.History].ToString();
            bool commentUpdated = false;
            CorrectImagePath(wi, wiItem, rev, ref currentComment, ref commentUpdated, isAttachmentMigratedDelegate);

            if (commentUpdated)
                wi.Fields[WiFieldReference.History] = currentComment;
        }
    }
}