using Common.Config;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Migration.Common;
using Migration.Common.Config;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Migration.Common.Log;

namespace WorkItemImport
{
    public class ImportCommandLine
    {
        private CommandLineApplication commandLineApplication;
        private string[] args;

        public ImportCommandLine(params string[] args)
        {
            InitCommandLine(args);
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

            commandLineApplication.OnExecute(() =>
            {
                bool forceFresh = forceOption.HasValue();

                if (configOption.HasValue())
                {
                    ExecuteMigration(tokenOption, urlOption, configOption, forceFresh);
                }
                else
                {
                    commandLineApplication.ShowHelp();
                }

                return 0;
            });
        }

        private void ExecuteMigration(CommandOption token, CommandOption url, CommandOption configFile, bool forceFresh)
        {
            ConfigJson config = null;
            var itemCount = 0;
            var importedItems = 0;
            var sw = new Stopwatch();
            sw.Start();
            try
            {
                string configFileName = configFile.Value();
                ConfigReaderJson configReaderJson = new ConfigReaderJson(configFileName);
                config = configReaderJson.Deserialize();

                var context = MigrationContext.Init("wi-import", new Dictionary<string, string>(), config.Workspace, config.LogLevel, forceFresh);

                //InitSession(configFileName, config, forceFresh);

                // connection settings for Azure DevOps/TFS:
                // full base url incl https, name of the project where the items will be migrated (if it doesn't exist on destination it will be created), personal access token
                var settings = new Settings(url.Value(), config.TargetProject, token.Value())
                {
                    BaseAreaPath = config.BaseAreaPath ?? string.Empty, // Root area path that will prefix area path of each migrated item
                    BaseIterationPath = config.BaseIterationPath ?? string.Empty, // Root iteration path that will prefix each iteration
                    IgnoreFailedLinks = config.IgnoreFailedLinks,
                    ProcessTemplate = config.ProcessTemplate
                };

                // initialize Azure DevOps/TFS connection. Creates/fetches project, fills area and iteration caches.
                var agent = Agent.Initialize(context, settings);

                if (agent == null)
                {
                    Logger.Log(LogLevel.Error, "Azure DevOps/TFS initialization error. Exiting...");
                    return;
                }

                var executionBuilder = new ExecutionPlanBuilder(context);
                var plan = executionBuilder.BuildExecutionPlan();

                itemCount = plan.ReferenceQueue.Count;

                BeginSession(configFileName, config, forceFresh, agent, itemCount);

                Console.WriteLine($"Found {itemCount} items to import.");
                while (plan.TryPop(out ExecutionPlan.ExecutionItem executionItem))
                {
                    try
                    {
                        if (!forceFresh && context.Journal.IsItemMigrated(executionItem.OriginId, executionItem.Revision.Index))
                            continue;

                        WorkItem wi = null;

                        if (executionItem.WiId > 0)
                            wi = agent.GetWorkItem(executionItem.WiId);
                        else
                            wi = agent.CreateWI(executionItem.WiType);

                        agent.ImportRevision(executionItem.Revision, wi);

                        importedItems++;
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
                            Logger.Log(ex);
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
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Error, $"Unexpected error: {e}");
            }
            finally
            {
                sw.Stop();
                EndSession(itemCount, importedItems, sw);
            }
        }

        //private static void InitSession(string configFile, ConfigJson config, bool force)
        //{
        //    var toolVersion = VersionInfo.GetVersionInfo();
        //    var osVersion = System.Runtime.InteropServices.RuntimeInformation.OSDescription.Trim();
        //    var machine = System.Environment.MachineName;
        //    var user = $"{System.Environment.UserDomainName}\\{System.Environment.UserName}";

        //    Logger.Init("wi-import",
        //        new Dictionary<string, string>() {
        //            { "Tool version         :", toolVersion },
        //            { "DateTime             :", DateTime.Now.ToString() },
        //            { "Telemetry            :", Logger.TelemetryStatus },
        //            { "Session Id           :", Logger.SessionId },
        //            { "Config               :", configFile },
        //            { "User                 :", user },
        //            { "Force                :", force ? "yes" : "no" },
        //            { "Machine              :", machine },
        //            { "System               :", osVersion },
        //            },
        //        config.Workspace, config.LogLevel);
        //}

        private static void BeginSession(string configFile, ConfigJson config, bool force, Agent agent, int itemsCount)
        {
            var toolVersion = VersionInfo.GetVersionInfo();
            var osVersion = System.Runtime.InteropServices.RuntimeInformation.OSDescription.Trim();
            var machine = System.Environment.MachineName;
            var user = $"{System.Environment.UserDomainName}\\{System.Environment.UserName}";

            Logger.StartSession("WorkItem Import", "wi-import-started",
            new Dictionary<string, string>() {
                { "Tool version         :", toolVersion },
                { "DateTime             :", DateTime.Now.ToString() },
                { "Telemetry            :", Logger.TelemetryStatus },
                { "Session Id           :", Logger.SessionId },
                { "Config               :", configFile },
                { "User                 :", user },
                { "Force                :", force ? "yes" : "no" },
                { "Machine              :", machine },
                { "System               :", osVersion },
                { "Azure DevOps url     :", agent.Settings.Account },
                { "Azure DevOps version :", "123" },
                { "Azure DevOps type    :", "Cloud" }
                },
            new Dictionary<string, string>() {
                { "item-count", itemsCount.ToString() },
                { "az-devops-version", "123" },
                { "az-devops-type", "Cloud" } });
        }

        private static void EndSession(int itemCount, int importedItemCount, Stopwatch sw)
        {
            Logger.EndSession("wi-import-completed",
                new Dictionary<string, string>() {
                    { "item-count", itemCount.ToString() },
                    { "imported-item-count", importedItemCount.ToString() },
                    { "error-count", Logger.Errors.ToString() },
                    { "warning-count", Logger.Warnings.ToString() },
                    { "elapsed-time", string.Format("{0:hh\\:mm\\:ss}", sw.Elapsed) } });
        }

        public void Run()
        {
            commandLineApplication.Execute(args);
        }
    }
}