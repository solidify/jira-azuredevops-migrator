﻿using Microsoft.TeamFoundation.Common;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Operations;
using Migration.Common;
using Migration.Common.Log;
using Migration.WIContract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VsWebApi = Microsoft.VisualStudio.Services.WebApi;
using WebApi = Microsoft.TeamFoundation.WorkItemTracking.WebApi;

namespace WorkItemImport
{
    public class Agent
    {
        private readonly MigrationContext _context;
        public Settings Settings { get; private set; }
        public VsWebApi.VssConnection RestConnection { get; private set; }
        public Dictionary<string, int> IterationCache { get; private set; } = new Dictionary<string, int>();
        public int RootIteration { get; private set; }
        public Dictionary<string, int> AreaCache { get; private set; } = new Dictionary<string, int>();
        public int RootArea { get; private set; }
        private readonly Dictionary<string, string> _iterationPathMap = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _areaPathMap = new Dictionary<string, string>();

        private WitClientUtils _witClientUtils;
        private WebApi.WorkItemTrackingHttpClient _wiClient;
        public WebApi.WorkItemTrackingHttpClient WiClient
        {
            get
            {
                if (_wiClient == null)
                    _wiClient = RestConnection.GetClient<WebApi.WorkItemTrackingHttpClient>();

                return _wiClient;
            }
        }

        private Agent(MigrationContext context, Settings settings, VsWebApi.VssConnection restConn)
        {
            _context = context;
            Settings = settings;
            RestConnection = restConn;
        }

        public WorkItem GetWorkItem(int wiId)
        {
            return _witClientUtils.GetWorkItem(wiId);
        }

        public WorkItem CreateWorkItem(string type, bool suppressNotifications, DateTime createdDate, string createdBy)
        {
            return _witClientUtils.CreateWorkItem(type, suppressNotifications, createdDate, createdBy);
        }

        public bool ImportRevision(WiRevision rev, WorkItem wi, Settings settings)
        {
            var incomplete = false;
            try
            {
                if (rev.Index == 0)
                    _witClientUtils.EnsureClassificationFields(rev);

                _witClientUtils.EnsureDateFields(rev, wi);
                _witClientUtils.EnsureAuthorFields(rev);
                _witClientUtils.EnsureAssigneeField(rev, wi);
                _witClientUtils.EnsureFieldsOnStateChange(rev, wi);

                _witClientUtils.EnsureWorkItemFieldsInitialized(rev, wi);

                var attachmentMap = new Dictionary<string, WiAttachment>();
                if (rev.Attachments.Any() && !_witClientUtils.ApplyAttachments(rev, wi, attachmentMap, _context.Journal.IsAttachmentMigrated))
                    incomplete = true;

                if (rev.Fields.Any() && !UpdateWIFields(rev.Fields, wi))
                    incomplete = true;

                if (rev.Fields.Any() && !UpdateWIHistoryField(rev.Fields, wi))
                    incomplete = true;

                if (rev.Links.Any() && !ApplyAndSaveLinks(rev, wi, settings))
                    incomplete = true;

                if (incomplete)
                    Logger.Log(LogLevel.Warning, $"'{rev}' - not all changes were saved.");

                if (wi.Fields.ContainsKey(WiFieldReference.History) && !string.IsNullOrEmpty(wi.Fields[WiFieldReference.History].ToString()))
                {
                    Logger.Log(LogLevel.Debug, $"Correcting comments on '{rev}'.");
                    _witClientUtils.CorrectComment(wi, _context.GetItem(rev.ParentOriginId), rev, _context.Journal.IsAttachmentMigrated);
                }

                _witClientUtils.SaveWorkItemAttachments(rev, wi, settings);

                foreach (string attOriginId in rev.Attachments.Select(wiAtt => wiAtt.AttOriginId))
                {
                    if (attachmentMap.TryGetValue(attOriginId, out WiAttachment tfsAtt))
                        _context.Journal.MarkAttachmentAsProcessed(attOriginId, tfsAtt.AttOriginId);
                }

                if (rev.Attachments.Exists(a => a.Change == ReferenceChangeType.Added) && rev.AttachmentReferences)
                {
                    Logger.Log(LogLevel.Debug, $"Correcting description on separate revision on '{rev}'.");

                    try
                    {
                        _witClientUtils.CorrectDescription(wi, _context.GetItem(rev.ParentOriginId), rev, _context.Journal.IsAttachmentMigrated);
                    }
                    catch (AttachmentNotFoundException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex, $"Failed to correct description for '{wi.Id}', rev '{rev}'.");
                    }

                    if (wi.Fields.ContainsKey(WiFieldReference.AcceptanceCriteria) && !string.IsNullOrEmpty(wi.Fields[WiFieldReference.AcceptanceCriteria].ToString()))
                    {
                        Logger.Log(LogLevel.Debug, $"Correcting acceptance criteria on separate revision on '{rev}'.");

                        try
                        {
                            _witClientUtils.CorrectAcceptanceCriteria(wi, _context.GetItem(rev.ParentOriginId), rev, _context.Journal.IsAttachmentMigrated);
                        }
                        catch (AttachmentNotFoundException)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(ex, $"Failed to correct acceptance criteria for '{wi.Id}', rev '{rev}'.");
                        }
                    }

                    // Correct other HTMl fields than description
                    foreach (var field in settings.FieldMap.Fields)
                    {
                        if (
                            field.Mapper == "MapRendered"
                            && (field.For == "All" || field.For.Split(',').Contains(wi.Fields[WiFieldReference.WorkItemType]))
                            && (field.NotFor == null || !field.NotFor.Split(',').Contains(wi.Fields[WiFieldReference.WorkItemType]))
                            && wi.Fields.ContainsKey(field.Target)
                            && field.Target != WiFieldReference.Description
                        )
                        {
                            try
                            {
                                _witClientUtils.CorrectRenderedField(
                                    wi,
                                    _context.GetItem(rev.ParentOriginId),
                                    rev,
                                    field.Target,
                                    _context.Journal.IsAttachmentMigrated
                                );
                            }
                            catch (AttachmentNotFoundException)
                            {
                                throw;
                            }
                            catch (Exception ex)
                            {
                                Logger.Log(ex, $"Failed to correct description for '{wi.Id}', rev '{rev}'.");
                            }
                        }
                    }
                }

                // rev with a development link won't have meaningful information, skip saving fields
                if (rev.DevelopmentLink != null)
                {
                    if (settings.IncludeDevelopmentLinks)
                    {
                        _witClientUtils.SaveWorkItemArtifacts(rev, wi, settings);
                    }
                }
                else
                {
                    _witClientUtils.SaveWorkItemFields(wi, settings);
                }

                if (wi.Id.HasValue)
                {
                    _context.Journal.MarkRevProcessed(rev.ParentOriginId, wi.Id.Value, rev.Index);
                }
                else
                {
                    throw new MissingFieldException($"Work Item had no ID: {wi.Url}");
                }

                Logger.Log(LogLevel.Debug, $"Imported revision.");

                return true;
            }
            catch (AbortMigrationException)
            {
                throw;
            }
            catch (AttachmentNotFoundException)
            {
                throw;
            }
            catch (FileNotFoundException ex)
            {
                Logger.Log(LogLevel.Error, ex.Message);
                Logger.Log(LogLevel.Error, $"Failed to import revision '{rev.Index}' of '{rev.ParentOriginId}'.");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Log(ex, $"Failed to import revisions for '{wi.Id}'.");
                return false;
            }
        }

        #region Static
        internal static Agent Initialize(MigrationContext context, Settings settings)
        {
            var restConnection = EstablishRestConnection(settings);
            if (restConnection == null)
                return null;

            var agent = new Agent(context, settings, restConnection);

            var witClientWrapper = new WitClientWrapper(settings.Account, settings.Project, settings.Pat, settings.ChangedDateBumpMS);
            agent._witClientUtils = new WitClientUtils(witClientWrapper);

            // check if projects exists, if not create it
            var project = agent.GetOrCreateProjectAsync().Result;
            if (project == null)
            {
                Logger.Log(LogLevel.Critical, "Could not establish connection to the remote Azure DevOps/TFS project.");
                return null;
            }

            (var iterationCache, int rootIteration) = agent.CreateClasificationCacheAsync(settings.Project, TreeStructureGroup.Iterations).Result;
            if (iterationCache == null)
            {
                Logger.Log(LogLevel.Critical, "Could not build iteration cache.");
                return null;
            }

            agent.IterationCache = iterationCache;
            agent.RootIteration = rootIteration;

            (var areaCache, int rootArea) = agent.CreateClasificationCacheAsync(settings.Project, TreeStructureGroup.Areas).Result;
            if (areaCache == null)
            {
                Logger.Log(LogLevel.Critical, "Could not build area cache.");
                return null;
            }

            agent.AreaCache = areaCache;
            agent.RootArea = rootArea;

            return agent;
        }

        private static VsWebApi.VssConnection EstablishRestConnection(Settings settings)
        {
            try
            {
                Logger.Log(LogLevel.Info, "Connecting to Azure DevOps/TFS...");
                var credentials = new VssBasicCredential("", settings.Pat);
                var uri = new Uri(settings.Account);
                return new VsWebApi.VssConnection(uri, credentials);
            }
            catch (Exception ex)
            {
                Logger.Log(ex, $"Cannot establish connection to Azure DevOps/TFS.", LogLevel.Critical);
                return null;
            }
        }

        #endregion

        #region Setup

        internal async Task<TeamProject> GetOrCreateProjectAsync()
        {
            ProjectHttpClient projectClient = RestConnection.GetClient<ProjectHttpClient>();
            Logger.Log(LogLevel.Info, "Retrieving project info from Azure DevOps/TFS...");
            TeamProject project = null;

            try
            {
                project = await projectClient.GetProject(Settings.Project);
            }
            catch (Exception ex)
            {
                Logger.Log(ex, $"Failed to get Azure DevOps/TFS project '{Settings.Project}'.");
            }

            if (project == null)
                project = await CreateProject(Settings.Project, $"{Settings.ProcessTemplate} project for Jira migration", Settings.ProcessTemplate);

            return project;
        }

        internal async Task<TeamProject> CreateProject(string projectName, string projectDescription = "", string processName = "Scrum")
        {
            Logger.Log(LogLevel.Warning, $"Project '{projectName}' does not exist.");
            Console.WriteLine("Would you like to create one? (Y/N)");
            var answer = Console.ReadKey();
            if (answer.KeyChar != 'Y' && answer.KeyChar != 'y')
                return null;

            Logger.Log(LogLevel.Info, $"Creating project '{projectName}'.");

            // Setup version control properties
            Dictionary<string, string> versionControlProperties = new Dictionary<string, string>
            {
                [TeamProjectCapabilitiesConstants.VersionControlCapabilityAttributeName] = SourceControlTypes.Git.ToString()
            };

            // Setup process properties       
            ProcessHttpClient processClient = RestConnection.GetClient<ProcessHttpClient>();
            Guid processId = processClient.GetProcessesAsync().Result.Find(process => { return process.Name.Equals(processName, StringComparison.InvariantCultureIgnoreCase); }).Id;

            Dictionary<string, string> processProperaties = new Dictionary<string, string>
            {
                [TeamProjectCapabilitiesConstants.ProcessTemplateCapabilityTemplateTypeIdAttributeName] = processId.ToString()
            };

            // Construct capabilities dictionary
            Dictionary<string, Dictionary<string, string>> capabilities = new Dictionary<string, Dictionary<string, string>>
            {
                [TeamProjectCapabilitiesConstants.VersionControlCapabilityName] = versionControlProperties,
                [TeamProjectCapabilitiesConstants.ProcessTemplateCapabilityName] = processProperaties
            };

            // Construct object containing properties needed for creating the project
            TeamProject projectCreateParameters = new TeamProject()
            {
                Name = projectName,
                Description = projectDescription,
                Capabilities = capabilities
            };

            // Get a client
            ProjectHttpClient projectClient = RestConnection.GetClient<ProjectHttpClient>();

            TeamProject project = null;
            try
            {
                Logger.Log(LogLevel.Info, "Queuing project creation...");

                // Queue the project creation operation 
                // This returns an operation object that can be used to check the status of the creation
                OperationReference operation = await projectClient.QueueCreateProject(projectCreateParameters);

                // Check the operation status every 5 seconds (for up to 30 seconds)
                Operation completedOperation = WaitForLongRunningOperation(operation.Id, 5, 30).Result;

                // Check if the operation succeeded (the project was created) or failed
                if (completedOperation.Status == OperationStatus.Succeeded)
                {
                    // Get the full details about the newly created project
                    project = projectClient.GetProject(
                        projectCreateParameters.Name,
                        includeCapabilities: true,
                        includeHistory: true).Result;

                    Logger.Log(LogLevel.Info, $"Project created (ID: {project.Id})");
                }
                else
                {
                    Logger.Log(LogLevel.Error, "Project creation operation failed: " + completedOperation.ResultMessage);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex, "Exception during create project.", LogLevel.Critical);
            }

            return project;
        }

        private async Task<Operation> WaitForLongRunningOperation(Guid operationId, int interavalInSec = 5, int maxTimeInSeconds = 60, CancellationToken cancellationToken = default)
        {
            OperationsHttpClient operationsClient = RestConnection.GetClient<OperationsHttpClient>();
            DateTime expiration = DateTime.Now.AddSeconds(maxTimeInSeconds);
            int checkCount = 0;

            while (true)
            {
                Logger.Log(LogLevel.Info, $" Checking status ({checkCount++})... ");

                Operation operation = await operationsClient.GetOperation(operationId, cancellationToken);

                if (!operation.Completed)
                {
                    Logger.Log(LogLevel.Info, $"   Pausing {interavalInSec} seconds...");

                    await Task.Delay(interavalInSec * 1000);

                    if (DateTime.Now > expiration)
                    {
                        Logger.Log(LogLevel.Error, $"Operation did not complete in {maxTimeInSeconds} seconds.");
                    }
                }
                else
                {
                    return operation;
                }
            }
        }

        private async Task<(Dictionary<string, int>, int)> CreateClasificationCacheAsync(string project, TreeStructureGroup structureGroup)
        {
            try
            {
                Logger.Log(LogLevel.Info, $"Building {(structureGroup == TreeStructureGroup.Iterations ? "iteration" : "area")} cache...");
                WorkItemClassificationNode all = await WiClient.GetClassificationNodeAsync(project, structureGroup, null, 1000);

                var clasificationCache = new Dictionary<string, int>();

                if (all.Children != null && all.Children.Any())
                {
                    foreach (var iteration in all.Children)
                        CreateClasificationCacheRec(iteration, clasificationCache, "");
                }

                return (clasificationCache, all.Id);
            }
            catch (Exception ex)
            {
                Logger.Log(ex, $"Error while building {(structureGroup == TreeStructureGroup.Iterations ? "iteration" : "area")} cache.");
                return (null, -1);
            }
        }

        private void CreateClasificationCacheRec(WorkItemClassificationNode current, Dictionary<string, int> agg, string parentPath)
        {
            string fullName = !string.IsNullOrWhiteSpace(parentPath) ? parentPath + "/" + current.Name : current.Name;

            agg.Add(fullName, current.Id);
            Logger.Log(LogLevel.Debug, $"{(current.StructureType == TreeNodeStructureType.Iteration ? "Iteration" : "Area")} '{fullName}' added to cache");
            if (current.Children != null)
            {
                foreach (var node in current.Children)
                    CreateClasificationCacheRec(node, agg, fullName);
            }
        }

        public string EnsureClasification(
            string fullName,
            TreeStructureGroup structureGroup = TreeStructureGroup.Iterations
        )
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                Logger.Log(LogLevel.Error, "Empty value provided for node name/path.");
                throw new ArgumentException("fullName");
            }

            var pathSplit = fullName.Split('/');
            var name = pathSplit[pathSplit.Length - 1];
            var parent = string.Join("/", pathSplit.Take(pathSplit.Length - 1));

            string nameMapped = "";
            string fullNameMapped = "";

            if (structureGroup == TreeStructureGroup.Iterations)
            {
                nameMapped = GetMappedClassificationNodePath(_iterationPathMap, name);
                fullNameMapped = parent.IsNullOrEmpty() ? nameMapped : $"{parent}/{nameMapped}";
            }
            else if (structureGroup == TreeStructureGroup.Areas)
            {
                nameMapped = GetMappedClassificationNodePath(_areaPathMap, name);
                fullNameMapped = parent.IsNullOrEmpty() ? nameMapped : $"{parent}/{nameMapped}";
            }
            else
            {
                Logger.Log(LogLevel.Error, $"Invalid tree structure group: {structureGroup}");
            }

            if (!string.IsNullOrEmpty(parent))
                EnsureClasification(parent, structureGroup);

            var cache = structureGroup == TreeStructureGroup.Iterations ? IterationCache : AreaCache;

            lock (cache)
            {
                if (cache.TryGetValue(fullNameMapped, out int id))
                    return fullNameMapped;

                WorkItemClassificationNode node = null;

                try
                {
                    node = WiClient.CreateOrUpdateClassificationNodeAsync(
                        new WorkItemClassificationNode() { Name = nameMapped, }, Settings.Project, structureGroup, parent).Result;
                }
                catch (Exception ex)
                {
                    Logger.Log(ex, $"Error while adding {(structureGroup == TreeStructureGroup.Iterations ? "iteration" : "area")} '{fullNameMapped}' to Azure DevOps/TFS.", LogLevel.Warning);
                }

                if (node != null)
                {
                    Logger.Log(LogLevel.Debug, $"{(structureGroup == TreeStructureGroup.Iterations ? "Iteration" : "Area")} '{fullNameMapped}' added to Azure DevOps/TFS.");
                    cache.Add(fullNameMapped, node.Id);
                    return fullNameMapped;
                }
            }
            return null;
        }

        // Ensure that classification nodes with conflicting names in ADO are migrated with unique names.
        // ADO Classification nodes are case insensitive
        private string GetMappedClassificationNodePath(Dictionary<string, string> dictionary, string name)
        {
            if (!dictionary.ContainsKey(name))
            {
                string nameUpdated = name;
                bool newSprintNameInIterationPathCaseInvariant = false;
                int suffix = 0;
                while (!newSprintNameInIterationPathCaseInvariant)
                {
                    if (!DictionaryContainsValueCaseInvariant(dictionary, nameUpdated))
                    {
                        newSprintNameInIterationPathCaseInvariant = true;
                        dictionary[name] = nameUpdated;
                    }
                    suffix += 1;
                    nameUpdated = $"{name}-{suffix}";
                }
            }
            name = dictionary[name];
            return name;
        }

        private bool DictionaryContainsValueCaseInvariant(Dictionary<string, string> dictionary, string name)
        {
            foreach (var value in dictionary.Values)
            {
                if (value.ToLower() == name.ToLower())
                {
                    return true;
                }
            }
            return false;
        }


        #endregion

        #region Import Revision

        private bool UpdateWIHistoryField(IEnumerable<WiField> fields, WorkItem wi)
        {
            if (fields.FirstOrDefault(i => i.ReferenceName == WiFieldReference.History) == null)
            {
                wi.Fields.Remove(WiFieldReference.History);
            }
            return true;
        }

        private bool UpdateWIFields(IEnumerable<WiField> fields, WorkItem wi)
        {
            var success = true;

            foreach (var fieldRev in fields)
            {
                try
                {
                    var fieldRef = fieldRev.ReferenceName;
                    var fieldValue = fieldRev.Value;


                    switch (fieldRef)
                    {
                        case var s when s.Equals(WiFieldReference.IterationPath, StringComparison.InvariantCultureIgnoreCase):

                            var iterationPath = Settings.BaseIterationPath;

                            if (!string.IsNullOrWhiteSpace((string)fieldValue))
                            {
                                if (string.IsNullOrWhiteSpace(iterationPath))
                                    iterationPath = (string)fieldValue;
                                else
                                    iterationPath = string.Join("/", iterationPath, (string)fieldValue);
                            }

                            if (!string.IsNullOrWhiteSpace(iterationPath))
                            {
                                string iterationPathMapped = EnsureClasification(iterationPath, TreeStructureGroup.Iterations);
                                wi.Fields[WiFieldReference.IterationPath] = $@"{Settings.Project}\{iterationPathMapped}".Replace("/", @"\");
                            }
                            else
                            {
                                wi.Fields[WiFieldReference.IterationPath] = Settings.Project;
                            }
                            Logger.Log(LogLevel.Debug, $"Mapped IterationPath '{wi.Fields[WiFieldReference.IterationPath]}'.");
                            break;

                        case var s when s.Equals(WiFieldReference.AreaPath, StringComparison.InvariantCultureIgnoreCase):

                            var areaPath = Settings.BaseAreaPath;

                            if (!string.IsNullOrWhiteSpace((string)fieldValue))
                            {
                                if (string.IsNullOrWhiteSpace(areaPath))
                                    areaPath = (string)fieldValue;
                                else
                                    areaPath = string.Join("/", areaPath, (string)fieldValue);
                            }

                            if (!string.IsNullOrWhiteSpace(areaPath))
                            {
                                string areaPathMapped = EnsureClasification(areaPath, TreeStructureGroup.Areas);
                                wi.Fields[WiFieldReference.AreaPath] = $@"{Settings.Project}\{areaPathMapped}".Replace("/", @"\");
                            }
                            else
                            {
                                wi.Fields[WiFieldReference.AreaPath] = Settings.Project;
                            }

                            Logger.Log(LogLevel.Debug, $"Mapped AreaPath '{wi.Fields[WiFieldReference.AreaPath]}'.");

                            break;

                        case var s when s.Equals(WiFieldReference.ActivatedDate, StringComparison.InvariantCultureIgnoreCase) && fieldValue == null ||
                            s.Equals(WiFieldReference.ActivatedBy, StringComparison.InvariantCultureIgnoreCase) && fieldValue == null ||
                            s.Equals(WiFieldReference.ClosedDate, StringComparison.InvariantCultureIgnoreCase) && fieldValue == null ||
                            s.Equals(WiFieldReference.ClosedBy, StringComparison.InvariantCultureIgnoreCase) && fieldValue == null ||
                            s.Equals(WiFieldReference.Tags, StringComparison.InvariantCultureIgnoreCase) && fieldValue == null:

                            Logger.Log(LogLevel.Info, $"Field '{s}' was null on the work item. Omitting...");
                            break;
                        case var s when s.Equals(WiFieldReference.ChangedDate, StringComparison.InvariantCultureIgnoreCase):
                            break;
                        default:
                            if (fieldValue != null)
                            {
                                wi.Fields[fieldRef] = fieldValue;
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex, $"Failed to update fields.");
                    success = false;
                }
            }

            return success;
        }

        private bool ApplyAndSaveLinks(WiRevision rev, WorkItem wi, Settings settings)
        {
            bool success = true;

            var saveLinkTimestamp = rev.Time;
            if(rev.Fields.Count > 0)
            {
                // If this revision already has any fields, defer the link import by 2 miliseconds. Otherwise the Work Items API will
                // send the response: "VS402625: Dates must be increasing with each revision"
                saveLinkTimestamp = saveLinkTimestamp.AddMilliseconds(2);
                // This needs to be set so that ApplyFields gets a later date, later on in the revision import
                wi.Fields[WiFieldReference.ChangedDate] = saveLinkTimestamp.AddMilliseconds(2);
            }
            
            for (int i = 0; i < rev.Links.Count; i++)
            {
                var link = rev.Links[i];
                try
                {
                    int sourceWiId = _context.Journal.GetMigratedId(link.SourceOriginId);
                    int targetWiId = _context.Journal.GetMigratedId(link.TargetOriginId);

                    link.SourceWiId = sourceWiId;
                    link.TargetWiId = targetWiId;

                    if (link.TargetWiId == -1)
                    {
                        var errorLevel = Settings.IgnoreFailedLinks ? LogLevel.Warning : LogLevel.Error;
                        Logger.Log(errorLevel, $"'{link}' - target work item for Jira '{link.TargetOriginId}'" +
                            $" is not yet created in Azure DevOps/TFS. You can safely ignore this warning if" +
                            $" this work item is scheduled for import later in your migration.");
                        success = false;
                        continue;
                    }

                    if (i > 0)
                    {
                        // If this has multiple link updates, defer each ubsequent link import by 2 miliseconds.
                        // Otherwise the Work Items API will send the response: "VS402625: Dates must be increasing with each revision"
                        saveLinkTimestamp = saveLinkTimestamp.AddMilliseconds(2);
                        // This needs to be set so that ApplyFields gets a later date, later on in the revision import
                        wi.Fields[WiFieldReference.ChangedDate] = saveLinkTimestamp.AddMilliseconds(2);
                    }

                    if (link.Change == ReferenceChangeType.Added && !_witClientUtils.AddAndSaveLink(link, wi, settings, rev.Author, saveLinkTimestamp))
                    {
                        success = false;
                    }
                    else if (link.Change == ReferenceChangeType.Removed && !_witClientUtils.RemoveAndSaveLink(link, wi, settings, rev.Author, saveLinkTimestamp))
                    {
                        success = false;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex, $"Failed to apply links for '{wi.Id}'.");
                    success = false;
                }
            }

            if (settings.IncludeLinkComments)
            {
                if (rev.Links.Exists(l => l.Change == ReferenceChangeType.Removed))
                    wi.Fields[WiFieldReference.History] = $"Removed link(s): {string.Join(";", rev.Links.Where(l => l.Change == ReferenceChangeType.Removed).Select(l => l.ToString()))}";
                else if (rev.Links.Exists(l => l.Change == ReferenceChangeType.Added))
                    wi.Fields[WiFieldReference.History] = $"Added link(s): {string.Join(";", rev.Links.Where(l => l.Change == ReferenceChangeType.Added).Select(l => l.ToString()))}";
            }

            return success;
        }
        #endregion
    }
}
