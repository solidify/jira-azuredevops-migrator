using Common.Config;
using Microsoft.Extensions.CommandLineUtils;
using Migration.Common.Config;
using Migration.Common.Log;
using Migration.WIContract;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
            CommandOption tokenOption = commandLineApplication.Option("-t <token>", "Bearer token for OAuth2 authentication", CommandOptionType.SingleValue);
            CommandOption urlOption = commandLineApplication.Option("--url <accounturl>", "Url for the account", CommandOptionType.SingleValue);
            CommandOption configOption = commandLineApplication.Option("--config <configurationfilename>", "Export the work items based on this configuration file", CommandOptionType.SingleValue);
            CommandOption forceOption = commandLineApplication.Option("--force", "Forces execution from start (instead of continuing from previous run)", CommandOptionType.NoValue);
            CommandOption continueOnCriticalOption = commandLineApplication.Option("--continue", "Continue execution upon a critical error", CommandOptionType.SingleValue);

            commandLineApplication.OnExecute(() =>
            {
                bool forceFresh = forceOption.HasValue();
                bool succeeded = true;

                if (configOption.HasValue())
                {
                    succeeded = ExecuteMigration(userOption, passwordOption, tokenOption, urlOption, configOption, forceFresh, continueOnCriticalOption);
                }
                else
                {
                    commandLineApplication.ShowHelp();
                }

                return succeeded ? 0 : -1;
            });
        }

        private bool ExecuteMigration(CommandOption user, CommandOption password, CommandOption token, CommandOption url, CommandOption configFile, bool forceFresh, CommandOption continueOnCritical)
        {
            var itemsCount = 0;
            var exportedItemsCount = 0;
            var sw = new Stopwatch();
            bool succeeded = true;
            sw.Start();
            var exportIssuesSummary = new ExportIssuesSummary();

            try
            {
                string configFileName = configFile.Value();
                ConfigReaderJson configReaderJson = new ConfigReaderJson(configFileName);
                var config = configReaderJson.Deserialize();

                InitSession(config, continueOnCritical.Value());

                // Migration session level settings
                // where the logs and journal will be saved, logs aid debugging, journal is for recovery of interupted process
                string migrationWorkspace = config.Workspace;

                var downloadOptions = (DownloadOptions)config.DownloadOptions;

                var jiraSettings = new JiraSettings(user.Value(), password.Value(), token.Value(), url.Value(), config.SourceProject)
                {
                    BatchSize = config.BatchSize,
                    UserMappingFile = config.UserMappingFile != null ? Path.Combine(migrationWorkspace, config.UserMappingFile) : string.Empty,
                    AttachmentsDir = Path.Combine(migrationWorkspace, config.AttachmentsFolder),
                    JQL = config.Query,
                    UsingJiraCloud = config.UsingJiraCloud,
                    IncludeDevelopmentLinks = config.IncludeDevelopmentLinks,
                    RepositoryMap = config.RepositoryMap
                };

                var jiraServiceWrapper = new JiraServiceWrapper(jiraSettings);
                JiraProvider jiraProvider = new JiraProvider(jiraServiceWrapper);
                jiraProvider.Initialize(jiraSettings, exportIssuesSummary);

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

                var mapper = new JiraMapper(jiraProvider, config, exportIssuesSummary);
                var localProvider = new WiItemProvider(migrationWorkspace);
                var exportedKeys = new HashSet<string>(Directory.EnumerateFiles(migrationWorkspace, "*.json").Select(f => Path.GetFileNameWithoutExtension(f)));
                var skips = forceFresh ? new HashSet<string>(Enumerable.Empty<string>()) : exportedKeys;

                var issues = jiraProvider.EnumerateIssues(jiraSettings.JQL, skips, downloadOptions);

                var createdWorkItems = new List<WiItem>();

                foreach (var issue in issues)
                {
                    if (issue == null)
                        continue;

                    WiItem wiItem = mapper.Map(issue);
                    if (wiItem != null)
                    {
                        createdWorkItems.Add(wiItem);
                    }
                }

                FixRevisionDates(createdWorkItems);

                foreach (var wiItem in createdWorkItems)
                {
                    localProvider.Save(wiItem);
                    exportedItemsCount++;
                    Logger.Log(LogLevel.Debug, $"Exported as type '{wiItem.Type}'.");
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
                EndSession(exportedItemsCount, sw, exportIssuesSummary);
            }
            return succeeded;
        }

        private static void FixRevisionDates(List<WiItem> createdWorkItems)
        {
            var revisionsWithLinkChanges = new List<WiRevision>();
            foreach (var wiItem in createdWorkItems)
            {
                revisionsWithLinkChanges.AddRange(wiItem.Revisions.Where(r => r.Links != null && r.Links.Count != 0));
            }
            bool anyRevisionTimeUpdated = true;
            while (anyRevisionTimeUpdated)
            {
                anyRevisionTimeUpdated = false;
                foreach (var rev1 in revisionsWithLinkChanges)
                {
                    var rev1LinkSourceWiIds = rev1.Links.Select(l => l.SourceOriginId).ToList();
                    var revsWithOppositeLink = revisionsWithLinkChanges.Where(
                        r => r.Links.Exists(l => rev1LinkSourceWiIds.Contains(l.TargetOriginId))
                    );
                    foreach (var rev2 in revsWithOppositeLink)
                    {
                        if (rev2 != rev1)
                        {
                            foreach (var link1 in rev1.Links)
                            {
                                foreach (var link2 in rev2.Links)
                                {
                                    if (link1.SourceOriginId == link2.TargetOriginId || link1.TargetOriginId == link2.SourceOriginId)
                                    {
                                        if (link1.Change == ReferenceChangeType.Added && link2.Change == ReferenceChangeType.Removed
                                            && rev1.Time < rev2.Time && Math.Abs((rev1.Time - rev2.Time).TotalSeconds) < 2
                                            && link1.WiType == "System.LinkTypes.Hierarchy-Forward" && link2.WiType == "System.LinkTypes.Hierarchy-Reverse")
                                        {
                                            // rev1 should be moved back by 1 second
                                            rev2.Time = rev2.Time.AddSeconds(-1);
                                            anyRevisionTimeUpdated = true;
                                        }
                                        else if (link1.Change == ReferenceChangeType.Removed && link2.Change == ReferenceChangeType.Added
                                            && rev1.Time > rev2.Time && Math.Abs((rev1.Time - rev2.Time).TotalSeconds) < 2
                                            && link1.WiType == "System.LinkTypes.Hierarchy-Reverse" && link2.WiType == "System.LinkTypes.Hierarchy-Forward")
                                        {
                                            // rev2 should be moved back by 1 second
                                            rev1.Time = rev1.Time.AddSeconds(-1);
                                            anyRevisionTimeUpdated = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void InitSession(ConfigJson config, string continueOnCritical)
        {
            Logger.Init("jira-export", config.Workspace, config.LogLevel, continueOnCritical);
        }

        private static void BeginSession(string configFile, ConfigJson config, bool force, JiraProvider jiraProvider, int itemsCount)
        {
            var toolVersion = VersionInfo.GetVersionInfo();
            var osVersion = System.Runtime.InteropServices.RuntimeInformation.OSDescription.Trim();
            var machine = System.Environment.MachineName;
            var user = $"{System.Environment.UserDomainName}\\{System.Environment.UserName}";
            var jiraVersion = jiraProvider.GetJiraVersion();

            Logger.Log(LogLevel.Info, $"Export started. Selecting {itemsCount} items.");

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

        private static void EndSession(int exportedItemsCount, Stopwatch sw, ExportIssuesSummary exportIssuesSummary)
        {
            sw.Stop();

            Logger.Log(LogLevel.Info, $"Export complete. Exported {exportedItemsCount} items ({Logger.Errors} errors, {Logger.Warnings} warnings) in {string.Format("{0:hh\\:mm\\:ss}", sw.Elapsed)}.");

            string issuesReportString = exportIssuesSummary.GetReportString();
            if (issuesReportString != "")
            {
                Logger.Log(LogLevel.Warning, issuesReportString);
            }

            Logger.EndSession("jira-export-completed",
                new Dictionary<string, string>() {
                    { "item-count", exportedItemsCount.ToString() },
                    { "error-count", Logger.Errors.ToString() },
                    { "warning-count", Logger.Warnings.ToString() },
                    { "elapsed-time", string.Format("{0:hh\\:mm\\:ss}", sw.Elapsed) }});
        }

        public int Run()
        {
            return commandLineApplication.Execute(args);
        }
    }
}