using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

using Migration.Common;
using Migration.Common.Log;
using Migration.WIContract;

namespace WorkItemImport
{
    public class WitClientUtils
    {
        private readonly IWitClientWrapper _witClientWrapper;

        public WitClientUtils(IWitClientWrapper witClientWrapper)
        {
            _witClientWrapper = witClientWrapper;
        }

        public delegate V IsAttachmentMigratedDelegate<in T, U, out V>(T input, out U output);

        public WorkItem CreateWorkItem(string type, DateTime createdDate = default, string createdBy = "")
        {
            return _witClientWrapper.CreateWorkItem(type, createdDate, createdBy);
        }

        public bool IsDuplicateWorkItemLink(IEnumerable<WorkItemRelation> links, WorkItemRelation relatedLink)
        {
            if (links == null || relatedLink == null)
            {
                return false;
            }

            var containsRelatedLink = links.Contains(relatedLink);
            var hasSameRelatedWorkItemId = links.OfType<WorkItemRelation>()
                .Any(l => l.Url == relatedLink.Url);

            if (!containsRelatedLink && !hasSameRelatedWorkItemId)
                return false;

            Logger.Log(LogLevel.Warning, $"Duplicate work item link detected to related workitem id: {relatedLink.Url}, Skipping link");
            return true;
        }

        public bool AddAndSaveLink(WiLink link, WorkItem wi)
        {
            if (link == null)
            {
                throw new ArgumentException(nameof(link));
            }
            if (wi == null)
            {
                throw new ArgumentException(nameof(wi));
            }

            WorkItemRelationType parsedLink = ParseLink(link);

            if (parsedLink != null)
            {
                try
                {
                    WorkItem targetWorkItem = GetWorkItem(link.TargetWiId);

                    WorkItemRelation relatedLink = new WorkItemRelation();
                    relatedLink.Rel = parsedLink.ReferenceName;
                    relatedLink.Url = targetWorkItem.Url;

                    relatedLink = ResolveCyclicalLinks(relatedLink, wi);
                    if (!IsDuplicateWorkItemLink(wi.Relations, relatedLink))
                    {
                        wi.Relations.Add(relatedLink);
                        AddSingleLinkToWorkItemAndSave(link, wi, targetWorkItem, "Imported link from JIRA");
                        return true;
                    }
                    return false;
                }
                catch (AggregateException ex)
                {
                    Logger.Log(LogLevel.Error, ex.Message);
                    foreach (Exception ex2 in ex.InnerExceptions)
                    {
                        Logger.Log(LogLevel.Error, ex2.Message);
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, ex.Message);
                    return false;
                }
            }
            else
                return false;

        }

        public bool RemoveAndSaveLink(WiLink link, WorkItem wi)
        {
            if (link == null)
            {
                throw new ArgumentException(nameof(link));
            }
            if (wi == null)
            {
                throw new ArgumentException(nameof(wi));
            }

            WorkItemRelation linkToRemove = wi.Relations.OfType<WorkItemRelation>().SingleOrDefault(
                rl =>
                    rl.Rel == link.WiType
                    && GetRelatedWorkItemIdFromLink(rl) == link.TargetWiId);
            if (linkToRemove == null)
            {
                Logger.Log(LogLevel.Warning, $"{link.ToString()} - cannot identify link to remove for '{wi.Id}'.");
                return false;
            }
            RemoveSingleLinkFromWorkItemAndSave(link, wi);
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
            if (rev == null)
            {
                throw new ArgumentException(nameof(rev));
            }

            if (rev.Fields == null)
            {
                throw new ArgumentException(nameof(rev.Fields));
            }

            if (wi == null)
            {
                throw new ArgumentException(nameof(wi));
            }

            string assignedTo = "";
            if(wi.Fields.ContainsKey(WiFieldReference.AssignedTo))
            {
                assignedTo = (wi.Fields[WiFieldReference.AssignedTo] as IdentityRef).UniqueName;
            }

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
            if (rev == null)
            {
                throw new ArgumentException(nameof(rev));
            }

            if (rev.Fields == null)
            {
                throw new ArgumentException(nameof(rev.Fields));
            }

            if (wi == null)
            {
                throw new ArgumentException(nameof(wi));
            }

            if (rev.Index == 0 && !rev.Fields.HasAnyByRefName(WiFieldReference.CreatedDate))
            {
                rev.Fields.Add(new WiField() { ReferenceName = WiFieldReference.CreatedDate, Value = rev.Time.ToString("o") });
            }
            if (!rev.Fields.HasAnyByRefName(WiFieldReference.ChangedDate))
            {
                DateTime workItemChangedDate = (DateTime)wi.Fields[WiFieldReference.ChangedDate];
                if (workItemChangedDate.ToUniversalTime() < rev.Time.ToUniversalTime())
                {
                    rev.Fields.Add(new WiField() { ReferenceName = WiFieldReference.ChangedDate, Value = rev.Time.ToString("o") });
                }
                else if (workItemChangedDate.ToUniversalTime() == rev.Time.ToUniversalTime())
                {
                    rev.Fields.Add(new WiField() { ReferenceName = WiFieldReference.ChangedDate, Value = rev.Time.AddMilliseconds(1).ToString("o") });
                }
                else
                {
                    rev.Fields.Add(new WiField() { ReferenceName = WiFieldReference.ChangedDate, Value = workItemChangedDate.ToString("o") });
                    // The work item ChangeDate is altered when saving the attachment, make sure the Revision time does too.
                    // Otherwise it will not be an increased ChangedDate and we'll get an exception
                    rev.Time = workItemChangedDate;
                }
            }

        }

        public void EnsureFieldsOnStateChange(WiRevision rev, WorkItem wi)
        {
            if (rev == null)
            {
                throw new ArgumentException(nameof(rev));
            }

            if (rev.Fields == null)
            {
                throw new ArgumentException(nameof(rev.Fields));
            }

            if (wi == null)
            {
                throw new ArgumentException(nameof(wi));
            }

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

        public void EnsureWorkItemFieldsInitialized(WiRevision rev, WorkItem wi)
        {
            if (rev == null)
            {
                throw new ArgumentException(nameof(rev));
            }

            if (rev.Fields == null)
            {
                throw new ArgumentException(nameof(rev.Fields));
            }

            // System.Title
            if (rev.Fields.HasAnyByRefName(WiFieldReference.Title))
            {
                string title = rev.Fields.GetFieldValueOrDefault<string>(WiFieldReference.Title);
                wi.Fields[WiFieldReference.Title] = title;
            }

            // System.Description
            string descriptionFieldRef = wi.Fields[WiFieldReference.WorkItemType].ToString() == "Bug" ? WiFieldReference.ReproSteps : WiFieldReference.Description;
            if (!wi.Fields.ContainsKey(descriptionFieldRef))
                    wi.Fields[descriptionFieldRef] = "";
        }

        public bool ApplyAttachments(WiRevision rev, WorkItem wi, Dictionary<string, WiAttachment> attachmentMap, IsAttachmentMigratedDelegate<string, string, bool> isAttachmentMigratedDelegate)
        {
            if (rev == null)
            {
                throw new ArgumentException(nameof(rev));
            }

            if (wi == null)
            {
                throw new ArgumentException(nameof(wi));
            }

            var success = true;

            foreach (var att in rev.Attachments)
            {
                try
                {
                    Logger.Log(LogLevel.Debug, $"Adding attachment '{att.ToString()}'.");
                    if (att.Change == ReferenceChangeType.Added)
                    {
                        AddRemoveAttachment(wi, att.AttOriginId, att.Comment, AttachmentOperation.ADD);

                        attachmentMap.Add(att.AttOriginId, att);
                    }
                    else if (att.Change == ReferenceChangeType.Removed)
                    {
                        WorkItemRelation existingAttachment = IdentifyAttachment(att, wi, isAttachmentMigratedDelegate);
                        if (existingAttachment != null)
                        {
                            AddRemoveAttachment(wi, att.AttOriginId, att.Comment, AttachmentOperation.REMOVE);
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

        private enum AttachmentOperation
        {
            ADD,
            REMOVE
        }

        private void AddRemoveAttachment(WorkItem wi, string attOriginId, string comment, AttachmentOperation op)
        {
            if (wi == null)
            {
                throw new ArgumentException(nameof(wi));
            }
            if (op == AttachmentOperation.ADD)
            {
                WorkItemRelation attachmentRelation = new WorkItemRelation();
                attachmentRelation.Rel = "AttachedFile";
                attachmentRelation.Attributes = new Dictionary<string, object>();
                attachmentRelation.Attributes["comment"] = comment;
                wi.Relations.Add(attachmentRelation);
            } else {
                WorkItemRelation attachmentRelation = wi.Relations.FirstOrDefault(
                    a => a.Rel == "AttachedFile" &&
                    a.Attributes["comment"].ToString().Split(
                        new string[] { ", original ID: " }, StringSplitOptions.None)[1] == attOriginId
                );
                if(attachmentRelation != default(WorkItemRelation))
                {
                    wi.Relations.Remove(attachmentRelation);
                }
            }
        }

        public bool CorrectDescription(WorkItem wi, WiItem wiItem, WiRevision rev, IsAttachmentMigratedDelegate<string, string, bool> isAttachmentMigratedDelegate)
        {
            if (wi == null)
            {
                throw new ArgumentException(nameof(wi));
            }

            if (wiItem == null)
            {
                throw new ArgumentException(nameof(wiItem));
            }

            if (rev == null)
            {
                throw new ArgumentException(nameof(rev));
            }

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

        public void CorrectComment(WorkItem wi, WiItem wiItem, WiRevision rev, IsAttachmentMigratedDelegate<string, string, bool> isAttachmentMigratedDelegate)
        {
            if (wi == null)
            {
                throw new ArgumentException(nameof(wi));
            }

            if (wiItem == null)
            {
                throw new ArgumentException(nameof(wiItem));
            }

            if (rev == null)
            {
                throw new ArgumentException(nameof(rev));
            }

            string currentComment = wi.Fields[WiFieldReference.History].ToString();
            bool commentUpdated = false;
            CorrectImagePath(wi, wiItem, rev, ref currentComment, ref commentUpdated, isAttachmentMigratedDelegate);

            if (commentUpdated)
                wi.Fields[WiFieldReference.History] = currentComment;
        }

        public WorkItem GetWorkItem(int wiId)
        {
            return _witClientWrapper.GetWorkItem(wiId);
        }

        public void SaveWorkItemAttachments(WiRevision rev, WorkItem wi)
        {
            if (rev == null)
            {
                throw new ArgumentException(nameof(rev));
            }

            if (wi == null)
            {
                throw new ArgumentException(nameof(wi));
            }

            // Calculate UpdatedTime
            DateTime attachmentUpdatedDate = rev.Time;
            DateTime workItemChangedDate = (DateTime)wi.Fields[WiFieldReference.ChangedDate];
            if (workItemChangedDate.ToUniversalTime() > rev.Time.ToUniversalTime())
            {
                attachmentUpdatedDate = workItemChangedDate;
                // The work item ChangeDate is altered when saving the attachment, make sure the Revision time does too.
                // Otherwise it will not be an increased ChangedDate and we'll get an exception
                rev.Time = workItemChangedDate;
            }

            // Save attachments
            foreach (WiAttachment attachment in rev.Attachments)
            {
                if (attachment.Change == ReferenceChangeType.Added)
                {
                    AddSingleAttachmentToWorkItemAndSave(attachment, wi, attachmentUpdatedDate, rev.Author);
                }
                else if (attachment.Change == ReferenceChangeType.Removed)
                {
                    RemoveSingleAttachmentFromWorkItemAndSave(attachment, wi, attachmentUpdatedDate, rev.Author);
                }
            }
        }

        public void SaveWorkItemFields(WorkItem wi)
        {
            if (wi == null)
            {
                throw new ArgumentException(nameof(wi));
            }

            // Build json patch document from fields
            JsonPatchDocument patchDocument = new JsonPatchDocument();
            foreach (string key in wi.Fields.Keys)
            {
                if (new string[] { 
                    WiFieldReference.BoardColumn,
                    WiFieldReference.BoardColumnDone,
                    WiFieldReference.BoardLane,
                }.Contains(key))
                    continue;

                object val = wi.Fields[key];

                patchDocument.Add(
                    new JsonPatchOperation()
                    {
                        Operation = Operation.Add,
                        Path = "/fields/" + key,
                        Value = val
                    }
                );
            }

            try
            {
                if (wi.Id.HasValue)
                    _witClientWrapper.UpdateWorkItem(patchDocument, wi.Id.Value);
                else
                    throw new MissingFieldException($"Work item ID was null: {wi.Url}");
            }
            catch (AggregateException ex)
            {
                foreach (Exception ex2 in ex.InnerExceptions)
                {
                    Logger.Log(LogLevel.Error, ex2.Message);
                }
                Logger.Log(LogLevel.Error, "Work Item " + wi.Id + " failed to save.");
            }
        }

        private void CorrectImagePath(WorkItem wi, WiItem wiItem, WiRevision rev, ref string textField, ref bool isUpdated, IsAttachmentMigratedDelegate<string, string, bool> isAttachmentMigratedDelegate)
        {
            if (wi == null)
            {
                throw new ArgumentException(nameof(wi));
            }

            if (wiItem == null)
            {
                throw new ArgumentException(nameof(wiItem));
            }

            if (rev == null)
            {
                throw new ArgumentException(nameof(rev));
            }

            foreach (var att in wiItem.Revisions.SelectMany(r => r.Attachments.Where(a => a.Change == ReferenceChangeType.Added)))
            {
                var fileName = att.FilePath.Split('\\')?.Last() ?? string.Empty;
                var encodedFileName = HttpUtility.UrlEncode(fileName);
                if (textField.Contains(fileName) || textField.IndexOf(encodedFileName, StringComparison.OrdinalIgnoreCase) >= 0 || textField.Contains("_thumb_" + att.AttOriginId))
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

        private void AddSingleAttachmentToWorkItemAndSave(WiAttachment att, WorkItem wi, object changedDate = default, object changedBy = default)
        {
            // Upload attachment
            AttachmentReference attachment = _witClientWrapper.CreateAttachment(att);
            Logger.Log(LogLevel.Info, "Attachment created");
            Logger.Log(LogLevel.Info, $"ID: { attachment.Id}");
            Logger.Log(LogLevel.Info, $"URL: '{attachment.Url}'");
            Logger.Log(LogLevel.Info, "");

            // Get an existing work item and add the attachment to it
            JsonPatchDocument attachmentPatchDocument = new JsonPatchDocument
            {
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/relations/-",
                    Value = new
                    {
                        rel = "AttachedFile",
                        url = attachment.Url,
                        attributes = new
                        {
                            comment = $"{att.Comment}, original ID: {att.AttOriginId}"
                        }
                    }
                }
            };

            if (changedDate != default)
            {
                attachmentPatchDocument.Add(new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/" + WiFieldReference.ChangedDate,
                    Value = changedDate
                });
            }

            if (changedBy != default)
            {
                attachmentPatchDocument.Add(new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/" + WiFieldReference.ChangedBy,
                    Value = changedBy
                });
            }

            var attachments = wi.Relations?.Where(r => r.Rel == "AttachedFile") ?? new List<WorkItemRelation>();
            var previousAttachmentsCount = attachments.Count();

            WorkItem result = null;
            if (wi.Id.HasValue)
                result = _witClientWrapper.UpdateWorkItem(attachmentPatchDocument, wi.Id.Value);
            else
                throw new MissingFieldException($"Work item ID was null: {wi.Url}");

            var newAttachments = result.Relations?.Where(r => r.Rel == "AttachedFile");
            var newAttachmentsCount = newAttachments.Count();

            Logger.Log(LogLevel.Info, $"Updated Existing Work Item: '{wi.Id}'. Had {previousAttachmentsCount} attachments, now has {newAttachmentsCount}");
            Logger.Log(LogLevel.Info, "");

            wi.Relations = result.Relations;

            // While updating the work item, the changed date can be increased, hence we take it over
            wi.Fields[WiFieldReference.ChangedDate] = result.Fields[WiFieldReference.ChangedDate];
        }

        private void RemoveSingleAttachmentFromWorkItemAndSave(WiAttachment att, WorkItem wi, DateTime changedDate = default, string changedBy = default)
        {
            WorkItemRelation existingAttachmentRelation =
                wi.Relations?.SingleOrDefault(
                    a => a.Rel == "AttachedFile" &&
                    a.Attributes["comment"].ToString().Split(
                        new string[] { ", original ID: " }, StringSplitOptions.None)[1] == att.AttOriginId
                );

            if(existingAttachmentRelation == null)
            {
                Logger.Log(LogLevel.Warning, $"Skipping saving attachment {att.AttOriginId}, since that attachment was not found.");
                return;
            }

            // Get an existing work item and add the attachment to it
            JsonPatchDocument attachmentPatchDocument = new JsonPatchDocument
            {
                new JsonPatchOperation()
                {
                    Operation = Operation.Remove,
                    Path = "/relations/-",
                    Value = new
                    {
                        rel = existingAttachmentRelation.Rel,
                        url = existingAttachmentRelation.Url
                    }
                }
            };

            if (changedDate != default)
            {
                attachmentPatchDocument.Add(new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/" + WiFieldReference.ChangedDate,
                    Value = changedDate
                });
            }

            if (changedBy != default)
            {
                attachmentPatchDocument.Add(new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/" + WiFieldReference.ChangedBy,
                    Value = changedBy
                });
            }

            IEnumerable<WorkItemRelation> existingAttachments = wi.Relations?.Where(r => r.Rel == "AttachedFile") ?? new List<WorkItemRelation>();
            int previousAttachmentsCount = existingAttachments.Count();

            WorkItem result = null;
            if (wi.Id.HasValue)
                result = _witClientWrapper.UpdateWorkItem(attachmentPatchDocument, wi.Id.Value);
            else
                throw new MissingFieldException($"Work item ID was null: {wi.Url}");

            IEnumerable<WorkItemRelation> newAttachments = result.Relations?.Where(r => r.Rel == "AttachedFile");
            int newAttachmentsCount = newAttachments.Count();

            Logger.Log(LogLevel.Info, $"Updated Existing Work Item: '{wi.Id}'. Had {previousAttachmentsCount} attachments, now has {newAttachmentsCount}");

            wi.Relations = result.Relations;
        }

        private void AddSingleLinkToWorkItemAndSave(WiLink link, WorkItem sourceWI, WorkItem targetWI, string comment)
        {
            // Create a patch document for a new work item.
            // Specify a relation to the existing work item.
            JsonPatchDocument linkPatchDocument = new JsonPatchDocument
            {
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/relations/-",
                    Value = new
                    {
                        rel = link.WiType,
                        url = targetWI.Url,
                        attributes = new
                        {
                            comment = comment
                        }
                    }
                }
            };

            if (sourceWI.Id.HasValue)
                _witClientWrapper.UpdateWorkItem(linkPatchDocument, sourceWI.Id.Value);
            else
                throw new MissingFieldException($"Work item ID was null: {sourceWI.Url}");

            Logger.Log(LogLevel.Info, $"Updated new work item Id:{sourceWI.Id} with link to work item ID:{targetWI.Id}");
        }

        private void RemoveSingleLinkFromWorkItemAndSave(WiLink link, WorkItem sourceWI)
        {
            WorkItemRelation rel = sourceWI.Relations.SingleOrDefault(a =>
                a.Rel == link.WiType
                && int.Parse(a.Url.Split('/').Last()) == link.TargetWiId);

            if (rel == null)
            {
                Logger.Log(LogLevel.Warning, "RemoveSingleLinkFromWorkItemAndSave(): rel was null");
                return;
            }

            int relIndex = sourceWI.Relations.IndexOf(rel);

            // Create a patch document for a new work item.
            // Specify a relation to the existing work item.
            JsonPatchDocument linkPatchDocument = new JsonPatchDocument
            {
                new JsonPatchOperation()
                {
                    Operation = Operation.Remove,
                    Path = "/relations/"+relIndex
                }
            };

            if (sourceWI.Id.HasValue)
                _witClientWrapper.UpdateWorkItem(linkPatchDocument, sourceWI.Id.Value);
            else
                throw new MissingFieldException($"Work item ID was null: {sourceWI.Url}");

            Logger.Log(LogLevel.Info, $"Updated new work item Id:{sourceWI.Id}, removed link with Url: {rel.Url}");
        }

        private WorkItemRelationType ParseLink(WiLink link)
        {
            List<WorkItemRelationType> linkTypes = _witClientWrapper.GetRelationTypes();
            WorkItemRelationType linkType = linkTypes.SingleOrDefault(lt => lt.ReferenceName == link.WiType);

            if (linkType == null)
            {
                Logger.Log(LogLevel.Error, $"'{link.ToString()}' - link type ({link.WiType}) does not exist in project");
            }
            return linkType;
        }

        private WorkItemRelation IdentifyAttachment(WiAttachment att, WorkItem wi, IsAttachmentMigratedDelegate<string, string, bool> isAttachmentMigratedDelegate)
        {
            if (isAttachmentMigratedDelegate(att.AttOriginId, out string attWiId))
            {
                return wi.Relations.SingleOrDefault(
                    a => a.Rel == "AttachedFile" &&
                    a.Attributes["comment"].ToString().Split(
                        new string[] { ", original ID: " }, StringSplitOptions.None)[1] == att.AttOriginId
                );
            }
            return null;
        }

        private WorkItemRelation ResolveCyclicalLinks(WorkItemRelation link, WorkItem wi)
        {
            if (DetectCycle(wi, link))
            {
                WorkItemRelation reverseLink = new WorkItemRelation();
                string reverseLinkTypeName = GetReverseLinkTypeReferenceName(link.Rel);
                reverseLink.Rel = reverseLinkTypeName;
                reverseLink.Url = link.Url.Replace(link.Url.Split('/').Last(), GetRelatedWorkItemIdFromLink(link).ToString());
                return reverseLink;
            }

            return link;
        }

        private bool DetectCycle(WorkItem startingWi, WorkItemRelation startingLink)
        {
            var nextWiLink = startingLink;
            do
            {
                var nextWi = GetWorkItem(GetRelatedWorkItemIdFromLink(nextWiLink));
                nextWiLink = nextWi.Relations.OfType<WorkItemRelation>().
                    Where(rl => rl.Rel != "AttachedFile").
                    FirstOrDefault(rl => GetRelatedWorkItemIdFromLink(rl) == startingWi.Id);

                if (nextWiLink != null && GetRelatedWorkItemIdFromLink(nextWiLink) == startingWi.Id)
                    return true;

            } while (nextWiLink != null);

            return false;
        }

        private string GetReverseLinkTypeReferenceName(string referenceName)
        {
            if (referenceName.Contains("Forward"))
            {
                return referenceName.Replace("Forward", "Reverse");
            }
            else
            {
                return referenceName.Replace("Reverse", "Forward");
            }
        }

        private int GetRelatedWorkItemIdFromLink(WorkItemRelation link)
        {
            return int.Parse(link.Url.Split('/').Last());
        }
    }
}