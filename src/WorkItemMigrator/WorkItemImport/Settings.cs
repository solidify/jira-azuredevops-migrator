using Migration.Common.Config;

namespace WorkItemImport
{
    public class Settings
    {
        public Settings(string account, string project, string pat)
        {
            Account = account;
            Project = project;
            Pat = pat;
        }

        public string Account { get; private set; }
        public string Project { get; private set; }
        public string Pat { get; private set; }
        public string BaseAreaPath { get; internal set; }
        public string BaseIterationPath { get; internal set; }
        public bool IgnoreFailedLinks { get; internal set; }
        public string ProcessTemplate { get; internal set; }
        public bool IncludeLinkComments { get; internal set; }
        public bool IncludeDevelopmentLinks { get; internal set; }
        public FieldMap FieldMap { get; internal set; }
        public bool SuppressNotifications { get; internal set; }
        public int BufferRevisionsSucceedingAttachmentImportsMilliseconds { get; set; }
    }
}