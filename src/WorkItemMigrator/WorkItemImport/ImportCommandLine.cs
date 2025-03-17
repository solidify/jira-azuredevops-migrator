using Common.Config;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Migration.Common;
using Migration.Common.Config;
using Migration.Common.Log;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace WorkItemImport
{
    public class ImportCommandLine
    {
        private CommandLineApplication commandLineApplication;
        private string[] args;
        private List<ExecutionPlan.ExecutionItem> deferredExecutionItems = new List<ExecutionPlan.ExecutionItem>();

        public ImportCommandLine(params string[] args)
        {
            InitCommandLine(args);
        }

        public int Run()
        {
            return commandLineApplication.Execute(args);
        }

        private void InitCommandLine(params string[] args)
        {
            commandLineApplication = new CommandLineApplication(throwOnUnexpectedArg: true);
            this.args = args;
            ConfigureCommandLineParserWithOptions();
        }

        private void ConfigureCommandLineParserWithOptions()
        {
            commandLineApplication.HelpOption("-? | -h | --help");
            commandLineApplication.FullName = "Work item migration tool that assists with moving Jira items to Azure DevOps or TFS.";
            commandLineApplication.Name = "wi-import";

            CommandOption tokenOption = commandLineApplication.Option("--token <accesstoken>", "Personal access token to use for authentication", CommandOptionType.SingleValue);
            CommandOption urlOption = commandLineApplication.Option("--url <accounturl>", "Url for the account", CommandOptionType.SingleValue);
            CommandOption configOption = commandLineApplication.Option("--config <configurationfilename>", "Import the work items based on the configuration file", CommandOptionType.SingleValue);
            CommandOption forceOption = commandLineApplication.Option("--force", "Forces execution from start (instead of continuing from previous run)", CommandOptionType.NoValue);
            CommandOption continueOnCriticalOption = commandLineApplication.Option("--continue", "Continue execution upon a critical error", CommandOptionType.SingleValue);

            commandLineApplication.OnExecute(() =>
            {
                bool forceFresh = forceOption.HasValue();
                bool succeeded = true;
                if (configOption.HasValue())
                {
                    succeeded = ExecuteMigration(tokenOption, urlOption, configOption, forceFresh, continueOnCriticalOption);
                }
                else
                {
                    commandLineApplication.ShowHelp();
                }

                return succeeded ? 0 : -1;
            });
        }

        private bool ExecuteMigration(CommandOption token, CommandOption url, CommandOption configFile, bool forceFresh, CommandOption continueOnCritical)
        {
            ConfigJson config = null;
            var itemCount = 0;
            var revisionCount = 0;
            var importedItems = 0;
            var sw = new Stopwatch();
            sw.Start();
            bool succeeded = true;

            try
            {
                string configFileName = configFile.Value();
                ConfigReaderJson configReaderJson = new ConfigReaderJson(configFileName);
                config = configReaderJson.Deserialize();

                var context = MigrationContext.Init("wi-import", config, config.LogLevel, forceFresh, continueOnCritical.Value());

                // connection settings for Azure DevOps/TFS:
                // full base url incl https, name of the project where the items will be migrated (if it doesn't exist on destination it will be created), personal access token
                var settings = new Settings(url.Value(), config.TargetProject, token.Value())
                {
                    BaseAreaPath = config.BaseAreaPath ?? string.Empty, // Root area path that will prefix area path of each migrated item
                    BaseIterationPath = config.BaseIterationPath ?? string.Empty, // Root iteration path that will prefix each iteration
                    IgnoreFailedLinks = config.IgnoreFailedLinks,
                    ProcessTemplate = config.ProcessTemplate,
                    IncludeLinkComments = config.IncludeLinkComments,
                    IncludeDevelopmentLinks = config.IncludeDevelopmentLinks,
                    FieldMap = config.FieldMap,
                    SuppressNotifications = config.SuppressNotifications,
                    ChangedDateBumpMS = config.ChangedDateBumpMS
                };

                // initialize Azure DevOps/TFS connection. Creates/fetches project, fills area and iteration caches.
                var agent = Agent.Initialize(context, settings);

                if (agent == null)
                {
                    Logger.Log(LogLevel.Critical, "Azure DevOps/TFS initialization error.");
                    return false;
                }

                var executionBuilder = new ExecutionPlanBuilder(context);
                var plan = executionBuilder.BuildExecutionPlan();

                itemCount = plan.ReferenceQueue.AsEnumerable().Select(x => x.OriginId).Distinct().Count();
                revisionCount = plan.ReferenceQueue.Count;

                BeginSession(configFileName, config, forceFresh, agent, itemCount, revisionCount);

                while (plan.ReferenceQueue.Count > 0 || deferredExecutionItems.Count > 0)
                {
                    ExecutionPlan.ExecutionItem executionItem = null;
                    try
                    {
                        executionItem = GetDeferredItemIfAvailable(plan, executionItem);

                        if (executionItem == null)
                        {
                            plan.TryPop(out executionItem);
                        }

                        if (
                            !forceFresh
                            && !executionItem.isDeferred
                            && context.Journal.IsItemMigrated(executionItem.OriginId, executionItem.Revision.Index)
                        )
                        {
                            continue;
                        }

                        WorkItem wi = null;

                        if (executionItem.WiId > 0)
                        {
                            wi = agent.GetWorkItem(executionItem.WiId);
                            if (wi == null)
                            {
                                Logger.Log(LogLevel.Error, $"Tried fetching work item with id={executionItem.WiId}, " +
                                    "but that work item does not exist on the target ADO organization/collection. " +
                                    "Perhaps the item has been deleted manually? If so, the ItemsJournal.txt file " +
                                    "is no longer valid. Please delete the work items in the target project and " +
                                    "rerun the wi-import, or run the import with --force enabled."
                                );
                                continue;
                            }
                        }
                        else
                        {
                            wi = agent.CreateWorkItem(executionItem.WiType, settings.SuppressNotifications, executionItem.Revision.Time, executionItem.Revision.Author);
                        }

                        Logger.Log(LogLevel.Info, $"Processing {importedItems + 1}/{revisionCount} - wi '{(wi.Id > 0 ? wi.Id.ToString() : "Initial revision")}', jira '{executionItem.OriginId}, rev {executionItem.Revision.Index}'.");

                        importedItems++;

                        if (config.IgnoreEmptyRevisions &&
                            executionItem.Revision.Fields.Count == 0 &&
                            executionItem.Revision.Links.Count == 0 &&
                            executionItem.Revision.Attachments.Count == 0 &&
                            executionItem.Revision.DevelopmentLink == null)
                        {
                            Logger.Log(LogLevel.Info, $"Skipped processing empty revision: {executionItem.OriginId}, rev {executionItem.Revision.Index}");
                            continue;
                        }

                        try
                        {
                            agent.ImportRevision(executionItem.Revision, wi, settings);
                        }
                        catch (AttachmentNotFoundException)
                        {
                            importedItems = DeferItem(importedItems, executionItem);
                        }

                        // Artifical wait (optional) to avoid throttling for ADO Services
                        if (config.SleepTimeBetweenRevisionImportMilliseconds > 0)
                        {
                            Thread.Sleep(config.SleepTimeBetweenRevisionImportMilliseconds);
                        }
                    }
                    catch (AbortMigrationException)
                    {
                        Logger.Log(LogLevel.Info, "Aborting migration...");
                        break;
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            Logger.Log(ex, $"Failed to import '{executionItem}'.");
                        }
                        catch (AbortMigrationException)
                        {
                            break;
                        }
                    }
                }
            }
            catch (CommandParsingException e)
            {
                Logger.Log(LogLevel.Error, $"Invalid command line option(s): {e}");
                succeeded = false;
            }
            catch (Exception e)
            {
                Logger.Log(e, $"Unexpected migration error.");
                succeeded = false;
            }
            finally
            {
                EndSession(itemCount, revisionCount, sw);
            }
            return succeeded;
        }

        private int DeferItem(int importedItems, ExecutionPlan.ExecutionItem executionItem)
        {
            if (!executionItem.isDeferred)
            {
                executionItem.Revision.Time = executionItem.Revision.Time.AddMinutes(5);
                executionItem.isDeferred = true;
                deferredExecutionItems.Add(executionItem);
                importedItems--;
            }

            return importedItems;
        }

        private ExecutionPlan.ExecutionItem GetDeferredItemIfAvailable(ExecutionPlan plan, ExecutionPlan.ExecutionItem executionItem)
        {
            foreach (var executionItemDeferred in deferredExecutionItems)
            {
                if (plan.TryPeek(out var nextItem))
                {
                    if (executionItemDeferred.Revision.Time < nextItem.Revision.Time)
                    {
                        executionItem = executionItemDeferred;
                        deferredExecutionItems.Remove(executionItem);
                        executionItem.Revision.Time = nextItem.Revision.Time.AddMilliseconds(-5);
                        break;
                    }
                }
                else
                {
                    executionItem = executionItemDeferred;
                    deferredExecutionItems.Remove(executionItem);
                    break;
                }
            }

            return executionItem;
        }

        private static void BeginSession(string configFile, ConfigJson config, bool force, Agent agent, int itemsCount, int revisionCount)
        {
            var toolVersion = VersionInfo.GetVersionInfo();
            var osVersion = System.Runtime.InteropServices.RuntimeInformation.OSDescription.Trim();
            var machine = System.Environment.MachineName;
            var user = $"{System.Environment.UserDomainName}\\{System.Environment.UserName}";
            var hostingType = GetHostingType(agent);

            Logger.Log(LogLevel.Info, $"Import started. Importing {itemsCount} items with {revisionCount} revisions.");

            Logger.StartSession("Azure DevOps Work Item Import",
                "wi-import-started",
                new Dictionary<string, string>() {
                    { "Tool version         :", toolVersion },
                    { "Start time           :", DateTime.Now.ToString() },
                    { "Telemetry            :", Logger.TelemetryStatus },
                    { "Session id           :", Logger.SessionId },
                    { "Tool user            :", user },
                    { "Config               :", configFile },
                    { "User                 :", user },
                    { "Force                :", force ? "yes" : "no" },
                    { "Log level            :", config.LogLevel },
                    { "Machine              :", machine },
                    { "System               :", osVersion },
                    { "Azure DevOps url     :", agent.Settings.Account },
                    { "Azure DevOps version :", "n/a" },
                    { "Azure DevOps type    :", hostingType }
                    },
                new Dictionary<string, string>() {
                    { "item-count", itemsCount.ToString() },
                    { "revision-count", revisionCount.ToString() },
                    { "system-version", "n/a" },
                    { "hosting-type", hostingType } });
        }

        private static string GetHostingType(Agent agent)
        {
            var uri = new Uri(agent.Settings.Account);
            switch (uri.Host.ToLower())
            {
                case "dev.azure.com":
                case "visualstudio.com":
                    return "Cloud";
                default:
                    return "Server";
            }
        }

        private static void EndSession(int itemsCount, int revisionCount, Stopwatch sw)
        {
            sw.Stop();

            Logger.Log(LogLevel.Info, $"Import complete. Imported {itemsCount} items, {revisionCount} revisions ({Logger.Errors} errors, {Logger.Warnings} warnings) in {string.Format("{0:hh\\:mm\\:ss}", sw.Elapsed)}.");

            Logger.EndSession("wi-import-completed",
                new Dictionary<string, string>() {
                    { "item-count", itemsCount.ToString() },
                    { "revision-count", revisionCount.ToString() },
                    { "error-count", Logger.Errors.ToString() },
                    { "warning-count", Logger.Warnings.ToString() },
                    { "elapsed-time", string.Format("{0:hh\\:mm\\:ss}", sw.Elapsed) } });
        }
    }
}
