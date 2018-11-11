using System;

namespace JiraExport
{
    public class JiraLink : IEquatable<JiraLink>
    {
        public string SourceItem { get; internal set; }
        public string TargetItem { get; internal set; }
        public string LinkType { get; internal set; }

        public bool Equals(JiraLink other)
        {
            return SourceItem.Equals(other.SourceItem, StringComparison.InvariantCultureIgnoreCase)
                && TargetItem.Equals(other.TargetItem, StringComparison.InvariantCultureIgnoreCase)
                && LinkType.Equals(other.LinkType, StringComparison.InvariantCultureIgnoreCase);
        }

        public override string ToString()
        {
            return $"[{LinkType}] {SourceItem}->{TargetItem}";
        }
    }
}