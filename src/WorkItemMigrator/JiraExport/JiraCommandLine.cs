﻿using System;
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
using Migration.Common.Log;

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
            var itemsCount = 0;
            var exportedItemsCount = 0;

            try
            {
                string configFileName = configFile.Value();
                ConfigReaderJson configReaderJson = new ConfigReaderJson(configFileName);
                var config = configReaderJson.Deserialize();

                // Migration session level settings
                // where the logs and journal will be saved, logs aid debugging, journal is for recovery of interupted process
                string migrationWorkspace = config.Workspace;

                var downloadOptions = JiraProvider.DownloadOptions.IncludeParentEpics | JiraProvider.DownloadOptions.IncludeSubItems | JiraProvider.DownloadOptions.IncludeParents;

                InitSession(configFileName, migrationWorkspace, config, forceFresh);

                var jiraSettings = new JiraSettings(user.Value(), password.Value(), url.Value(), config.SourceProject)
                {
                    BatchSize = config.BatchSize,
                    UserMappingFile = config.UserMappingFile != null ? Path.Combine(migrationWorkspace, config.UserMappingFile) : string.Empty,
                    AttachmentsDir = Path.Combine(migrationWorkspace, config.AttachmentsFolder),
                    JQL = config.Query
                };

                JiraProvider jiraProvider = JiraProvider.Initialize(jiraSettings);

                itemsCount = jiraProvider.GetItemCount(jiraSettings.JQL);

                BeginSession(jiraProvider, itemsCount);

                // Get the custom field names for epic link field and sprint field
                jiraSettings.EpicLinkField = jiraProvider.GetCustomId(config.EpicLinkField);
                jiraSettings.SprintField = jiraProvider.GetCustomId(config.SprintField);

                var mapper = new JiraMapper(jiraProvider, config);
                var localProvider = new WiItemProvider(migrationWorkspace);
                var exportedKeys = new HashSet<string>(Directory.EnumerateFiles(migrationWorkspace, "*.json").Select(f => Path.GetFileNameWithoutExtension(f)));
                var skips = forceFresh ? new HashSet<string>(Enumerable.Empty<string>()) : exportedKeys;

                var issues = jiraProvider.EnumerateIssues(jiraSettings.JQL, skips, downloadOptions);

                foreach (var issue in issues)
                {
                    WiItem wiItem = mapper.Map(issue);
                    if (wiItem != null)
                    {
                        localProvider.Save(wiItem);
                        exportedItemsCount++;
                        Logger.Log(LogLevel.Info, $"Exported {wiItem.ToString()}");
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
                EndSession(itemsCount, exportedItemsCount);
            }
        }

        private static void InitSession(string configFile, string migrationWorkspace, ConfigJson config, bool force)
        {
            var toolVersion = VersionInfo.GetVersionInfo();
            var osVersion = System.Runtime.InteropServices.RuntimeInformation.OSDescription.Trim();
            var machine = System.Environment.MachineName;
            var user = $"{System.Environment.UserDomainName}\\{System.Environment.UserName}";

            Logger.Init("jira-export",
                new Dictionary<string, string>() {
                    { "Tool version :", toolVersion },
                    { "DateTime     :", DateTime.Now.ToString() },
                    { "Telemetry    :", Logger.TelemetryStatus },
                    { "Session Id   :", Logger.SessionId },
                    { "Config       :", configFile },
                    { "User         :", user },
                    { "Machine      :", machine },
                    { "System       :", osVersion },
                    },
                migrationWorkspace, config.LogLevel);
        }

        private static void BeginSession(JiraProvider jiraProvider, int itemsCount)
        {
            var jiraVersion = jiraProvider.GetJiraVersion();

            Logger.StartSession("jira-export-started",
                new Dictionary<string, string>() { 
                    { "Jira url     :", jiraProvider.Settings.Url },
                    { "Jira user    :", jiraProvider.Settings.UserID },
                    { "Jira version :", jiraVersion.Version },
                    { "Jira type    :", jiraVersion.DeploymentType }
                    },
                new Dictionary<string, string>() {
                    { "item-count", itemsCount.ToString() },
                    { "jira-version", jiraVersion.Version },
                    { "jira-type", jiraVersion.DeploymentType } });
        }

        private static void EndSession(int itemsCount, int exportedItemsCount)
        {
            Logger.EndSession("jira-export-completed", 
                new Dictionary<string, string>() {
                    { "item-count", itemsCount.ToString() },
                    { "exported-item-count", exportedItemsCount.ToString() },
                    { "error-count", Logger.Errors.ToString() },
                    { "warning-count", Logger.Warnings.ToString() } });
        }

        public void Run()
        {
            commandLineApplication.Execute(args);
        }
    }
}