using System;
using System.Collections.Generic;
using System.IO;

namespace Migration.Common
{
    public class Journal
    {
        #region Static methods

        internal static Journal Init(MigrationContext context)
        {
            var journal = new Journal(context);

            if (File.Exists(journal.ItemsPath) && context.ForceFresh)
                File.Delete(journal.ItemsPath);

            if (File.Exists(journal.AttachmentsPath) && context.ForceFresh)
                File.Delete(journal.AttachmentsPath);

            return Load(journal);
        }

        #endregion

        #region Parsing

        internal static Journal Load(Journal journal)
        {
            if (File.Exists(journal.ItemsPath))
            {
                string[] revLines = File.ReadAllLines(journal.ItemsPath);
                foreach (string rev in revLines)
                {
                    string[] props = rev.Split(';');
                    journal.ProcessedRevisions[props[0]] = (Convert.ToInt32(props[1]), Convert.ToInt32(props[2]));
                }
            }

            if (File.Exists(journal.AttachmentsPath))
            {
                string[] attLines = File.ReadAllLines(journal.AttachmentsPath);
                foreach (string att in attLines)
                {
                    string[] props = att.Split(';');
                    Guid guid0;
                    Guid.TryParse(props[0], out guid0);
                    Guid guid1;
                    Guid.TryParse(props[1], out guid1);
                    journal.ProcessedAttachments[guid0] = guid1;
                }
            }

            return journal;
        }

        #endregion

        public Dictionary<string, (int, int)> ProcessedRevisions { get; private set; } = new Dictionary<string, (int, int)>();

        public Dictionary<Guid, Guid> ProcessedAttachments { get; private set; } = new Dictionary<Guid, Guid>();
        public string ItemsPath { get; private set; }
        public string AttachmentsPath { get; private set; }

        public Journal(MigrationContext context)
        {
            ItemsPath = Path.Combine(context.MigrationWorkspace, "itemsJournal.txt");
            AttachmentsPath = Path.Combine(context.MigrationWorkspace, "attachmentsJournal.txt");
        }

        public void MarkRevProcessed(string originId, int wiId, int rev)
        {

            ProcessedRevisions[originId] = (wiId, rev);
            WriteItem(originId, wiId, rev);
        }

        private void WriteItem(string originId, int wiId, int rev)
        {
            File.AppendAllText(ItemsPath, $"{originId};{wiId};{rev}" + Environment.NewLine);
        }

        public void MarkAttachmentAsProcessed(Guid attOriginId, Guid attWiId)
        {
            ProcessedAttachments.Add(attOriginId, attWiId);
            WriteAttachment(attOriginId, attWiId);
        }

        private void WriteAttachment(Guid attOriginId, Guid attWiId)
        {
            File.AppendAllText(AttachmentsPath, $"{attOriginId};{attWiId}" + Environment.NewLine);
        }

        public bool IsItemMigrated(string originId, int rev)
        {
            if (!ProcessedRevisions.TryGetValue(originId, out (int, int) migrationResult))
                return false;
            (_, int migratedRev) = migrationResult;
            return rev <= migratedRev;
        }

        public int GetMigratedId(string originId)
        {
            if (!ProcessedRevisions.TryGetValue(originId, out (int, int) migrationResult))
                return -1;
            (int wiId, _) = migrationResult;

            return wiId;
        }

        public bool IsAttachmentMigrated(Guid attOriginId, out Guid attWiId)
        {
            return ProcessedAttachments.TryGetValue(attOriginId, out attWiId);
        }
    }
}
