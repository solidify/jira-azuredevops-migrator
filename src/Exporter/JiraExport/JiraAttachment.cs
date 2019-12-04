using System;

namespace JiraExport
{
    public sealed class JiraAttachment : IEquatable<JiraAttachment>
    {
        public string Id { get; internal set; }
        public string Filename { get; internal set; }
        public string Url { get; internal set; }
        public string ThumbUrl { get; internal set; }
        public string LocalPath { get; internal set; }
        public string LocalThumbPath { get; internal set; }

        public bool Equals(JiraAttachment other)
        {
            return Id == other.Id;
        }

        public override string ToString()
        {
            return $"{Id}/{Filename}";
        }
    }
}