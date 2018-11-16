using Common.Config;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Migration.Common;
using Migration.Common.Config;
using System;

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
            try
            {
                string configFileName = configFile.Value();
                ConfigReaderJson configReaderJson = new ConfigReaderJson(configFileName);
                config = configReaderJson.Deserialize();

                // Migration session level settings
                // where the logs and journal will be saved, logs aid debugging, journal is for recovery of interupted process
                string migrationWorkspace = config.Workspace;

                // level of log messages that will be let through to console
                LogLevel logLevel;
                switch (config.LogLevel)
                {
                    case "Info": logLevel = LogLevel.Info; break;
                    case "Debug": logLevel = LogLevel.Debug; break;
                    case "Warning": logLevel = LogLevel.Warning; break;
                    case "Error": logLevel = LogLevel.Error; break;
                    case "Critical": logLevel = LogLevel.Critical; break;
                    default: logLevel = LogLevel.Debug; break;
                }

                // set up log, journal and run session settings
                var context = MigrationContext.Init(migrationWorkspace, logLevel, forceFresh);

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
        }

        public void Run()
        {
            commandLineApplication.Execute(args);
        }
    }
}