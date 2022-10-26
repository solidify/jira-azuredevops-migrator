using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Operations;

using Migration.Common;
using Migration.Common.Log;
using Migration.WIContract;

using VsWebApi = Microsoft.VisualStudio.Services.WebApi;
using WebApi = Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using WebModel = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using System.IO;

namespace WorkItemImport
{
    public class Agent
    {
        private readonly MigrationContext _context;
        public Settings Settings { get; private set; }

        public TfsTeamProjectCollection Collection
        {
            get; private set;
        }

        public VsWebApi.VssConnection RestConnection { get; private set; }
        public Dictionary<string, int> IterationCache { get; private set; } = new Dictionary<string, int>();
        public int RootIteration { get; private set; }
        public Dictionary<string, int> AreaCache { get; private set; } = new Dictionary<string, int>();
        public int RootArea { get; private set; }

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

        private Agent(MigrationContext context, Settings settings, VsWebApi.VssConnection restConn, TfsTeamProjectCollection soapConnection)
        {
            _context = context;
            Settings = settings;
            RestConnection = restConn;
            Collection = soapConnection;
        }

        public WorkItem GetWorkItem(int wiId)
        {
            return _witClientUtils.GetWorkItem(wiId);
        }

        public WorkItem CreateWorkItem(string type)
        {
            return _witClientUtils.CreateWorkItem(type);
        }

        public bool ImportRevision(WiRevision rev, WorkItem wi)
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

                if (rev.Links.Any() && !ApplyAndSaveLinks(rev, wi))
                    incomplete = true;

                if (incomplete)
                    Logger.Log(LogLevel.Warning, $"'{rev.ToString()}' - not all changes were saved.");

                if (rev.Attachments.All(a => a.Change != ReferenceChangeType.Added) && rev.AttachmentReferences)
                {
                    Logger.Log(LogLevel.Debug, $"Correcting description on '{rev.ToString()}'.");
                    _witClientUtils.CorrectDescription(wi, _context.GetItem(rev.ParentOriginId), rev, _context.Journal.IsAttachmentMigrated);
                }
                if (wi.Fields.ContainsKey(WiFieldReference.History) && !string.IsNullOrEmpty(wi.Fields[WiFieldReference.History].ToString()))
                {
                    Logger.Log(LogLevel.Debug, $"Correcting comments on '{rev.ToString()}'.");
                    _witClientUtils.CorrectComment(wi, _context.GetItem(rev.ParentOriginId), rev, _context.Journal.IsAttachmentMigrated);
                }

                if (rev.Attachments.Any(a => a.Change == ReferenceChangeType.Added) && rev.AttachmentReferences)
                {
                    Logger.Log(LogLevel.Debug, $"Correcting description on separate revision on '{rev.ToString()}'.");

                    try
                    {
                        _witClientUtils.CorrectDescription(wi, _context.GetItem(rev.ParentOriginId), rev, _context.Journal.IsAttachmentMigrated);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex, $"Failed to correct description for '{wi.Id}', rev '{rev.ToString()}'.");
                    }
                }

                _witClientUtils.SaveWorkItem(rev, wi);

                foreach (string attOriginId in rev.Attachments.Select(wiAtt => wiAtt.AttOriginId))
                {
                    if (attachmentMap.TryGetValue(attOriginId, out WiAttachment tfsAtt))
                        _context.Journal.MarkAttachmentAsProcessed(attOriginId, tfsAtt.AttOriginId);
                }

                if (wi.Id.HasValue)
                {
                    _context.Journal.MarkRevProcessed(rev.ParentOriginId, wi.Id.Value, rev.Index);
                } else
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

            var soapConnection = EstablishSoapConnection(settings);
            if (soapConnection == null)
                return null;

            var agent = new Agent(context, settings, restConnection, soapConnection);

            var witClientWrapper = new WitClientWrapper(settings.Account, settings.Project, settings.Pat);
            agent._witClientUtils = new WitClientUtils(witClientWrapper);

            // check if projects exists, if not create it
            var project = agent.GetOrCreateProjectAsync().Result;
            if (project == null)
            {
                Logger.Log(LogLevel.Critical, "Could not establish connection to the remote Azure DevOps/TFS project.");
                return null;
            }

            (var iterationCache, int rootIteration) = agent.CreateClasificationCacheAsync(settings.Project, WebModel.TreeStructureGroup.Iterations).Result;
            if (iterationCache == null)
            {
                Logger.Log(LogLevel.Critical, "Could not build iteration cache.");
                return null;
            }

            agent.IterationCache = iterationCache;
            agent.RootIteration = rootIteration;

            (var areaCache, int rootArea) = agent.CreateClasificationCacheAsync(settings.Project, WebModel.TreeStructureGroup.Areas).Result;
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

        private static TfsTeamProjectCollection EstablishSoapConnection(Settings settings)
        {
            NetworkCredential netCred = new NetworkCredential(string.Empty, settings.Pat);
            VssBasicCredential basicCred = new VssBasicCredential(netCred);
            VssCredentials tfsCred = new VssCredentials(basicCred);
            var collection = new TfsTeamProjectCollection(new Uri(settings.Account), tfsCred);
            collection.Authenticate();
            return collection;
        }

        #endregion

        #region Setup

        internal async Task<TeamProject> GetOrCreateProjectAsync()
        {
            ProjectHttpClient projectClient = RestConnection.GetClient<ProjectHttpClient>();
            Logger.Log(LogLevel.Info, "Retreiving project info from Azure DevOps/TFS...");
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

        private async Task<Operation> WaitForLongRunningOperation(Guid operationId, int interavalInSec = 5, int maxTimeInSeconds = 60, CancellationToken cancellationToken = default(CancellationToken))
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

        private async Task<(Dictionary<string, int>, int)> CreateClasificationCacheAsync(string project, WebModel.TreeStructureGroup structureGroup)
        {
            try
            {
                Logger.Log(LogLevel.Info, $"Building {(structureGroup == WebModel.TreeStructureGroup.Iterations ? "iteration" : "area")} cache...");
                WebModel.WorkItemClassificationNode all = await WiClient.GetClassificationNodeAsync(project, structureGroup, null, 1000);

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
                Logger.Log(ex, $"Error while building {(structureGroup == WebModel.TreeStructureGroup.Iterations ? "iteration" : "area")} cache.");
                return (null, -1);
            }
        }

        private void CreateClasificationCacheRec(WebModel.WorkItemClassificationNode current, Dictionary<string, int> agg, string parentPath)
        {
            string fullName = !string.IsNullOrWhiteSpace(parentPath) ? parentPath + "/" + current.Name : current.Name;

            agg.Add(fullName, current.Id);
            Logger.Log(LogLevel.Debug, $"{(current.StructureType == WebModel.TreeNodeStructureType.Iteration ? "Iteration" : "Area")} '{fullName}' added to cache");
            if (current.Children != null)
            {
                foreach (var node in current.Children)
                    CreateClasificationCacheRec(node, agg, fullName);
            }
        }

        public int? EnsureClasification(string fullName, WebModel.TreeStructureGroup structureGroup = WebModel.TreeStructureGroup.Iterations)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                Logger.Log(LogLevel.Error, "Empty value provided for node name/path.");
                throw new ArgumentException("fullName");
            }

            var path = fullName.Split('/');
            var name = path.Last();
            var parent = string.Join("/", path.Take(path.Length - 1));

            if (!string.IsNullOrEmpty(parent))
                EnsureClasification(parent, structureGroup);

            var cache = structureGroup == WebModel.TreeStructureGroup.Iterations ? IterationCache : AreaCache;

            lock (cache)
            {
                if (cache.TryGetValue(fullName, out int id))
                    return id;

                WebModel.WorkItemClassificationNode node = null;

                try
                {
                    node = WiClient.CreateOrUpdateClassificationNodeAsync(
                        new WebModel.WorkItemClassificationNode() { Name = name, }, Settings.Project, structureGroup, parent).Result;
                }
                catch (Exception ex)
                {
                    Logger.Log(ex, $"Error while adding {(structureGroup == WebModel.TreeStructureGroup.Iterations ? "iteration" : "area")} '{fullName}' to Azure DevOps/TFS.", LogLevel.Critical);
                }

                if (node != null)
                {
                    Logger.Log(LogLevel.Debug, $"{(structureGroup == WebModel.TreeStructureGroup.Iterations ? "Iteration" : "Area")} '{fullName}' added to Azure DevOps/TFS.");
                    cache.Add(fullName, node.Id);
                    return node.Id;
                }
            }
            return null;
        }

        #endregion

        #region Import Revision

        private bool UpdateWIHistoryField(IEnumerable<WiField> fields, WorkItem wi)
        {
            if(fields.FirstOrDefault( i => i.ReferenceName == WiFieldReference.History ) == null )
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
                                EnsureClasification(iterationPath, WebModel.TreeStructureGroup.Iterations);
                                wi.Fields[WiFieldReference.IterationPath] = $@"{Settings.Project}\{iterationPath}".Replace("/", @"\");
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
                                EnsureClasification(areaPath, WebModel.TreeStructureGroup.Areas);
                                wi.Fields[WiFieldReference.AreaPath] = $@"{Settings.Project}\{areaPath}".Replace("/", @"\");
                            }
                            else
                            {
                                wi.Fields[WiFieldReference.AreaPath] = Settings.Project;
                            }

                            Logger.Log(LogLevel.Debug, $"Mapped AreaPath '{ wi.Fields[WiFieldReference.AreaPath] }'.");

                            break;

                        case var s when s.Equals(WiFieldReference.ActivatedDate, StringComparison.InvariantCultureIgnoreCase) && fieldValue == null ||
                             s.Equals(WiFieldReference.ActivatedBy, StringComparison.InvariantCultureIgnoreCase) && fieldValue == null ||
                            s.Equals(WiFieldReference.ClosedDate, StringComparison.InvariantCultureIgnoreCase) && fieldValue == null ||
                            s.Equals(WiFieldReference.ClosedBy, StringComparison.InvariantCultureIgnoreCase) && fieldValue == null ||
                            s.Equals(WiFieldReference.Tags, StringComparison.InvariantCultureIgnoreCase) && fieldValue == null:

                            wi.Fields[fieldRef] = fieldValue;
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

        private bool ApplyAndSaveLinks(WiRevision rev, WorkItem wi)
        {
            bool success = true;

            foreach (var link in rev.Links)
            {
                try
                {
                    int sourceWiId = _context.Journal.GetMigratedId(link.SourceOriginId);
                    int targetWiId = _context.Journal.GetMigratedId(link.TargetOriginId);

                    link.SourceWiId = sourceWiId;
                    link.TargetWiId = targetWiId;

                    if (link.TargetWiId == -1)
                    {
                        var errorLevel = Settings.IgnoreFailedLinks ? LogLevel.Warning : LogLevel.Error;
                        Logger.Log(errorLevel, $"'{link.ToString()}' - target work item for Jira '{link.TargetOriginId}' is not yet created in Azure DevOps/TFS.");
                        success = false;
                        continue;
                    }

                    if (link.Change == ReferenceChangeType.Added && !_witClientUtils.AddAndSaveLink(link, wi))
                    {
                        success = false;
                    }
                    else if (link.Change == ReferenceChangeType.Removed && !_witClientUtils.RemoveAndSaveLink(link, wi))
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

            if (rev.Links.Any(l => l.Change == ReferenceChangeType.Removed))
                wi.Fields[WiFieldReference.History] = $"Removed link(s): { string.Join(";", rev.Links.Where(l => l.Change == ReferenceChangeType.Removed).Select(l => l.ToString()))}";
            else if (rev.Links.Any(l => l.Change == ReferenceChangeType.Added))
                wi.Fields[WiFieldReference.History] = $"Added link(s): { string.Join(";", rev.Links.Where(l => l.Change == ReferenceChangeType.Added).Select(l => l.ToString()))}";

            return success;
        }

        #endregion
    }
}