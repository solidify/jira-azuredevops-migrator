using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Common.Config;

using Microsoft.Extensions.CommandLineUtils;

using Migration.Common.Config;
using Migration.Common.Log;
using Migration.WIContract;

using static JiraExport.JiraProvider;

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
            var sw = new Stopwatch();
            sw.Start();

            try
            {
                string configFileName = configFile.Value();
                ConfigReaderJson configReaderJson = new ConfigReaderJson(configFileName);
                var config = configReaderJson.Deserialize();

                InitSession(config);

                // Migration session level settings
                // where the logs and journal will be saved, logs aid debugging, journal is for recovery of interupted process
                string migrationWorkspace = config.Workspace;

                var downloadOptions = (DownloadOptions)config.DownloadOptions;

                var jiraSettings = new JiraSettings(user.Value(), password.Value(), url.Value(), config.SourceProject)
                {
                    BatchSize = config.BatchSize,
                    UserMappingFile = config.UserMappingFile != null ? Path.Combine(migrationWorkspace, config.UserMappingFile) : string.Empty,
                    AttachmentsDir = Path.Combine(migrationWorkspace, config.AttachmentsFolder),
                    JQL = config.Query,
                    UsingJiraCloud = config.UsingJiraCloud
                };

                JiraProvider jiraProvider = JiraProvider.Initialize(jiraSettings);

                itemsCount = jiraProvider.GetItemCount(jiraSettings.JQL);

                BeginSession(configFileName, config, forceFresh, jiraProvider, itemsCount);

                jiraSettings.EpicLinkField = jiraProvider.GetCustomId(config.EpicLinkField);
                if (string.IsNullOrEmpty(jiraSettings.EpicLinkField))
                {
                    Logger.Log(LogLevel.Warning, $"Epic link field missing for config field '{config.EpicLinkField}'.");
                }
                jiraSettings.SprintField = jiraProvider.GetCustomId(config.SprintField);
                if (string.IsNullOrEmpty(jiraSettings.SprintField))
                {
                    Logger.Log(LogLevel.Warning, $"Sprint link field missing for config field '{config.SprintField}'.");
                }

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
                        Logger.Log(LogLevel.Debug, $"Exported as type '{wiItem.Type}'.");
                    }
                }
            }
            catch (CommandParsingException e)
            {
                Logger.Log(LogLevel.Error, $"Invalid command line option(s): {e}");
            }
            catch (Exception e)
            {
                Logger.Log(e, $"Unexpected migration error.");
            }
            finally
            {
                EndSession(itemsCount, sw);
            }
        }

        private static void InitSession(ConfigJson config)
        {
            Logger.Init("jira-export", config.Workspace, config.LogLevel);
        }

        private static void BeginSession(string configFile, ConfigJson config, bool force, JiraProvider jiraProvider, int itemsCount)
        {
            var toolVersion = VersionInfo.GetVersionInfo();
            var osVersion = System.Runtime.InteropServices.RuntimeInformation.OSDescription.Trim();
            var machine = System.Environment.MachineName;
            var user = $"{System.Environment.UserDomainName}\\{System.Environment.UserName}";
            var jiraVersion = jiraProvider.GetJiraVersion();

            Logger.Log(LogLevel.Info, $"Export started. Exporting {itemsCount} items.");

            Logger.StartSession("Jira Export",
                "jira-export-started",
                new Dictionary<string, string>() {
                    { "Tool version :", toolVersion },
                    { "Start time   :", DateTime.Now.ToString() },
                    { "Telemetry    :", Logger.TelemetryStatus },
                    { "Session id   :", Logger.SessionId },
                    { "Tool user    :", user },
                    { "Config       :", configFile },
                    { "Force        :", force ? "yes" : "no" },
                    { "Log level    :", config.LogLevel },
                    { "Machine      :", machine },
                    { "System       :", osVersion },
                    { "Jira url     :", jiraProvider.Settings.Url },
                    { "Jira user    :", jiraProvider.Settings.UserID },
                    { "Jira version :", jiraVersion.Version },
                    { "Jira type    :", jiraVersion.DeploymentType }
                    },
                new Dictionary<string, string>() {
                    { "item-count", itemsCount.ToString() },
                    { "system-version", jiraVersion.Version },
                    { "hosting-type", jiraVersion.DeploymentType } });
        }

        private static void EndSession(int itemsCount, Stopwatch sw)
        {
            sw.Stop();

            Logger.Log(LogLevel.Info, $"Export complete. Exported {itemsCount} items ({Logger.Errors} errors, {Logger.Warnings} warnings) in {string.Format("{0:hh\\:mm\\:ss}", sw.Elapsed)}.");

            Logger.EndSession("jira-export-completed",
                new Dictionary<string, string>() {
                    { "item-count", itemsCount.ToString() },
                    { "error-count", Logger.Errors.ToString() },
                    { "warning-count", Logger.Warnings.ToString() },
                    { "elapsed-time", string.Format("{0:hh\\:mm\\:ss}", sw.Elapsed) }});
        }

        public void Run()
        {
            commandLineApplication.Execute(args);
        }
    }
}