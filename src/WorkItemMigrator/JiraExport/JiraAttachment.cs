using System;

namespace JiraExport
{
    public sealed class JiraAttachment : IEquatable<JiraAttachment>
    {
        public string Id { get; set; }
        public string Filename { get; set; }
        public string Url { get; set; }
        public string LocalPath { get; set; }

        public bool Equals(JiraAttachment other)
        {
            if (other == null)
                return false;
            return Id == other.Id;
        }

        public override string ToString()
        {
            return $"{Id}/{Filename}";
        }
    }
}