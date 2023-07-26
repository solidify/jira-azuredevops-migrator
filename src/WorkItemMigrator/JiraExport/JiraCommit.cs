using System;

namespace JiraExport
{
    public class JiraCommit
    {
        public string Repository { get; set; }
        public string Id { get; set; }
        public DateTime AuthorTimestamp { get; set; }
    }
}