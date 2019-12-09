﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using Migration.Common.Log;
using Migration.WIContract;

namespace Migration.Common
{
    public class MigrationContext
    {
        public static MigrationContext Instance { get; private set; }
        public string AttachmentsPath { get { return Path.Combine(MigrationWorkspace, "Attachments"); } }
        public string UserMappingPath { get { return Path.Combine(MigrationWorkspace, "users.txt"); } }
        public Dictionary<string, string> UserMapping { get; private set; }
        public string App { get; internal set; }
        public string MigrationWorkspace { get; internal set; }
        public LogLevel LogLevel { get; internal set; }
        public bool ForceFresh { get; internal set; }
        public Journal Journal { get; internal set; }
        public WiItemProvider Provider { get; private set; }

        private MigrationContext(string app, string workspacePath, string logLevel, bool forceFresh)
        {
            App = app;
            MigrationWorkspace = workspacePath;
            UserMapping = UserMapper.ParseUserMappings(UserMappingPath);
            LogLevel = Logger.GetLogLevelFromString(logLevel);
            ForceFresh = forceFresh;
        }

        public static MigrationContext Init(string app, string workspacePath, string logLevel, bool forceFresh, string continueOnCritical)
        {
            Instance = new MigrationContext(app, workspacePath, logLevel, forceFresh);

            Logger.Init(app, workspacePath, logLevel, continueOnCritical);

            Instance.Journal = Journal.Init(Instance);
            Instance.Provider = new WiItemProvider(Instance.MigrationWorkspace);

            if (!Directory.Exists(Instance.AttachmentsPath))
                Directory.CreateDirectory(Instance.AttachmentsPath);

            return Instance;
        }

        public WiItem GetItem(string originId)
        {
            var item = this.Provider.Load(originId);
            item.WiId = Journal.GetMigratedId(originId);
            foreach (var link in item.Revisions.SelectMany(r => r.Links))
            {
                link.SourceOriginId = item.OriginId;
                link.SourceWiId = Journal.GetMigratedId(originId);
            }

            return item;
        }

        public IEnumerable<WiItem> EnumerateAllItems()
        {
            var result = new List<WiItem>();

            foreach (WiItem item in this.Provider.EnumerateAllItems())
            {
                item.WiId = Journal.GetMigratedId(item.OriginId);
                result.Add(item);
            }

            return result;
        }
    }
}
