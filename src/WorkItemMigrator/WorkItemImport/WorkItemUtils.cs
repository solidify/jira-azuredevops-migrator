using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

using Migration.Common;
using Migration.Common.Log;
using Migration.WIContract;

namespace WorkItemImport
{
    public class WorkItemUtils
    {
        public delegate V IsAttachmentMigratedDelegate<T, U, V>(T input, out U output);

        public static void SetFieldValue(WorkItem wi, string fieldRef, object fieldValue)
        {
            try
            {
                var field = wi.Fields[fieldRef];

                field.Value = fieldValue;

                if (field.IsValid)
                    Logger.Log(LogLevel.Debug, $"Mapped '{fieldRef}' '{fieldValue}'.");
                else
                {
                    field.Value = null;
                    Logger.Log(LogLevel.Warning, $"Mapped empty value for '{fieldRef}', because value was not valid");
                }
            }
            catch (ValidationException ex)
            {
                Logger.Log(LogLevel.Error, ex.Message);
            }
        }

        public static bool IsDuplicateWorkItemLink(LinkCollection links, RelatedLink relatedLink)
        {
            if (links == null)
            {
                throw new ArgumentException(nameof(links));
            }

            if (relatedLink == null)
            {
                throw new ArgumentException(nameof(relatedLink));
            }

            var containsRelatedLink = links.Contains(relatedLink);
            var hasSameRelatedWorkItemId = links.OfType<RelatedLink>()
                .Any(l => l.RelatedWorkItemId == relatedLink.RelatedWorkItemId);

            if (!containsRelatedLink && !hasSameRelatedWorkItemId)
                return false;

            Logger.Log(LogLevel.Warning, $"Duplicate work item link detected to related workitem id: {relatedLink.RelatedWorkItemId}, Skipping link");
            return true;
        }

        public static WorkItemLinkTypeEnd ParseLinkEnd(WiLink link, WorkItem wi)
        {
            var props = link.WiType?.Split('-');
            var linkType = wi.Project.Store.WorkItemLinkTypes.SingleOrDefault(lt => lt.ReferenceName == props?[0]);
            if (linkType == null)
            {
                Logger.Log(LogLevel.Error, $"'{link.ToString()}' - link type ({props?[0]}) does not exist in project");
                return null;
            }

            WorkItemLinkTypeEnd linkEnd = null;

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

        public static bool RemoveLink(WiLink link, WorkItem wi)
        {
            var linkToRemove = wi.Links.OfType<RelatedLink>().SingleOrDefault(rl => rl.LinkTypeEnd.ImmutableName == link.WiType && rl.RelatedWorkItemId == link.TargetWiId);
            if (linkToRemove == null)
            {
                Logger.Log(LogLevel.Warning, $"{link.ToString()} - cannot identify link to remove for '{wi.Id}'.");
                return false;
            }
            wi.Links.Remove(linkToRemove);
            return true;
        }

        public static void EnsureAuthorFields(WiRevision rev)
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

        public static void EnsureAssigneeField(WiRevision rev, WorkItem wi)
        {
            string assignedTo = wi.Fields[WiFieldReference.AssignedTo].Value.ToString();

            if (rev.Fields.HasAnyByRefName(WiFieldReference.AssignedTo))
            {
                var field = rev.Fields.First(f => f.ReferenceName.Equals(WiFieldReference.AssignedTo, StringComparison.InvariantCultureIgnoreCase));
                assignedTo = field.Value?.ToString() ?? string.Empty;
                rev.Fields.RemoveAll(f => f.ReferenceName.Equals(WiFieldReference.AssignedTo, StringComparison.InvariantCultureIgnoreCase));
            }
            rev.Fields.Add(new WiField() { ReferenceName = WiFieldReference.AssignedTo, Value = assignedTo });
        }

        public static void EnsureDateFields(WiRevision rev, WorkItem wi)
        {
            if (rev.Index == 0 && !rev.Fields.HasAnyByRefName(WiFieldReference.CreatedDate))
            {
                rev.Fields.Add(new WiField() { ReferenceName = WiFieldReference.CreatedDate, Value = rev.Time.ToString("o") });
            }
            if (!rev.Fields.HasAnyByRefName(WiFieldReference.ChangedDate))
            {
                if (wi.ChangedDate == rev.Time)
                {
                    rev.Fields.Add(new WiField() { ReferenceName = WiFieldReference.ChangedDate, Value = rev.Time.AddMilliseconds(1).ToString("o") });
                }
                else
                    rev.Fields.Add(new WiField() { ReferenceName = WiFieldReference.ChangedDate, Value = rev.Time.ToString("o") });
            }

        }

        public static void EnsureFieldsOnStateChange(WiRevision rev, WorkItem wi)
        {
            if (rev.Index != 0 && rev.Fields.HasAnyByRefName(WiFieldReference.State))
            {
                var wiState = wi.Fields[WiFieldReference.State]?.Value?.ToString() ?? string.Empty;
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

        public static void EnsureClassificationFields(WiRevision rev)
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

        public static bool ApplyAttachments(WiRevision rev, WorkItem wi, Dictionary<string, Attachment> attachmentMap, IsAttachmentMigratedDelegate<string, int, bool> isAttachmentMigratedDelegate)
        {
            var success = true;

            if (!wi.IsOpen)
                wi.Open();

            foreach (var att in rev.Attachments)
            {
                try
                {
                    Logger.Log(LogLevel.Debug, $"Adding attachment '{att.ToString()}'.");
                    if (att.Change == ReferenceChangeType.Added)
                    {
                        var newAttachment = new Attachment(att.FilePath, att.Comment);
                        wi.Attachments.Add(newAttachment);

                        attachmentMap.Add(att.AttOriginId, newAttachment);
                    }
                    else if (att.Change == ReferenceChangeType.Removed)
                    {
                        Attachment existingAttachment = IdentifyAttachment(att, wi, isAttachmentMigratedDelegate);
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
                wi.Fields[CoreField.History].Value = $"Removed attachments(s): { string.Join(";", rev.Attachments.Where(a => a.Change == ReferenceChangeType.Removed).Select(a => a.ToString()))}";

            return success;
        }

        public static Attachment IdentifyAttachment(WiAttachment att, WorkItem wi, IsAttachmentMigratedDelegate<string, int, bool> isAttachmentMigratedDelegate)
        {
            //if (context.Journal.IsAttachmentMigrated(att.AttOriginId, out int attWiId))
            if (isAttachmentMigratedDelegate(att.AttOriginId, out int attWiId))
                return wi.Attachments.Cast<Attachment>().SingleOrDefault(a => a.Id == attWiId);
            return null;
        }

        public static void CorrectImagePath(WorkItem wi, WiItem wiItem, WiRevision rev, ref string textField, ref bool isUpdated, IsAttachmentMigratedDelegate<string, int, bool> isAttachmentMigratedDelegate)
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
                        textField = Regex.Replace(textField, imageSrcPattern, $"src=\"{tfsAtt.Uri.AbsoluteUri}\"");
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

                wi.Fields[WiFieldReference.ChangedDate].Value = changedDate;
                wi.Fields[WiFieldReference.ChangedBy].Value = rev.Author;
            }
        }

        public static bool CorrectDescription(WorkItem wi, WiItem wiItem, WiRevision rev, IsAttachmentMigratedDelegate<string, int, bool> isAttachmentMigratedDelegate)
        {
            string description = wi.Type.Name == "Bug" ? wi.Fields[WiFieldReference.ReproSteps].Value.ToString() : wi.Description;
            if (string.IsNullOrWhiteSpace(description))
                return false;

            bool descUpdated = false;

            CorrectImagePath(wi, wiItem, rev, ref description, ref descUpdated, isAttachmentMigratedDelegate);

            if (descUpdated)
            {
                if (wi.Type.Name == "Bug")
                {
                    wi.Fields[WiFieldReference.ReproSteps].Value = description;
                }
                else
                {
                    wi.Fields[WiFieldReference.Description].Value = description;
                }
            }

            return descUpdated;
        }

        public static void CorrectComment(WorkItem wi, WiItem wiItem, WiRevision rev, IsAttachmentMigratedDelegate<string, int, bool> isAttachmentMigratedDelegate)
        {
            var currentComment = wi.History;
            var commentUpdated = false;
            CorrectImagePath(wi, wiItem, rev, ref currentComment, ref commentUpdated, isAttachmentMigratedDelegate);

            if (commentUpdated)
                wi.Fields[CoreField.History].Value = currentComment;
        }
    }
}