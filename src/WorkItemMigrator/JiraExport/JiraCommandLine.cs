using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Common.Config;
using Microsoft.Extensions.CommandLineUtils;
using Migration.Common;
using Migration.Common.Config;
using Migration.WIContract;
using Newtonsoft.Json;

namespace JiraExport
{
    public class JiraCommandLine
    {
        private CommandLineApplication commandLineApplication;
        private string[] args;

        public JiraCommandLine(params string[] args)
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
            commandLineApplication.Name = "jira-export";

            CommandOption userOption = commandLineApplication.Option("-u <username>", "Username for authentication", CommandOptionType.SingleValue);
            CommandOption passwordOption = commandLineApplication.Option("-p <password>", "Password for authentication", CommandOptionType.SingleValue);
            CommandOption urlOption = commandLineApplication.Option("--url <accounturl>", "Url for the account", CommandOptionType.SingleValue);
            CommandOption configOption = commandLineApplication.Option("--config <configurationfilename>", "Export the work items based on this configuration file", CommandOptionType.SingleValue);
            CommandOption forceOption = commandLineApplication.Option("--force", "Forces execution from start (instead of continuing from previous run)", CommandOptionType.NoValue);

            commandLineApplication.OnExecute(() =>
            {
                bool forceFresh = forceOption.HasValue();

                if (configOption.HasValue())
                {
                    ExecuteMigration(userOption, passwordOption, urlOption, configOption, forceFresh);
                }
                else
                {
                    commandLineApplication.ShowHelp();
                }

                return 0;
            });
        }

        private void ExecuteMigration(CommandOption user, CommandOption password, CommandOption url, CommandOption configFile, bool forceFresh)
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

                var downloadOptions = JiraProvider.DownloadOptions.IncludeParentEpics | JiraProvider.DownloadOptions.IncludeSubItems | JiraProvider.DownloadOptions.IncludeParents;

                Logger.Init(migrationWorkspace, logLevel);

                var jiraSettings = new JiraSettings(user.Value(), password.Value(), url.Value(), config.SourceProject)
                {
                    BatchSize = config.BatchSize,
                    UserMappingFile = config.UserMappingFile != null ? Path.Combine(migrationWorkspace, config.UserMappingFile) : string.Empty,
                    AttachmentsDir = Path.Combine(migrationWorkspace, config.AttachmentsFolder),
                    EpicLinkField = config.EpicLinkField != null ? config.EpicLinkField : string.Empty,
                    SprintField = config.SprintField != null ? config.SprintField : string.Empty,
                    JQL = config.Query
                };

                JiraProvider jiraProvider = JiraProvider.Initialize(jiraSettings);
                var mapper = new JiraMapper(jiraProvider, config);
                var localProvider = new WiItemProvider(migrationWorkspace);
                var exportedKeys = new HashSet<string>(Directory.EnumerateFiles(migrationWorkspace, "*.json").Select(f => Path.GetFileNameWithoutExtension(f)));
                var skips = forceFresh ? new HashSet<string>(Enumerable.Empty<string>()) : exportedKeys;

                foreach (var issue in jiraProvider.EnumerateIssues(jiraSettings.JQL, skips, downloadOptions))
                {
                    WiItem wiItem = mapper.Map(issue);
                    localProvider.Save(wiItem);
                    Logger.Log(LogLevel.Info, $"Exported {wiItem.ToString()}");
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