using System;

namespace JiraExport
{
    public class JiraDevelopmentLink
    {
        public enum DevelopmentLinkType
        {
            Commit,
            Branch
        }

        public string Repository { get; private set; }
        public string Id { get; private set; }
        public DateTime AuthorTimestamp { get; private set; }
        public DevelopmentLinkType Type { get; private set; }

        public JiraDevelopmentLink(string repository, string id, DateTime authorTimestamp, DevelopmentLinkType type)
        {
            Repository = repository;
            Id = id;
            AuthorTimestamp = authorTimestamp;
            Type = type;
        }
    }
}