using System;

namespace WorkItemImport
{
    public class RevisionReference : IComparable<RevisionReference>, IEquatable<RevisionReference>
    {
        public string OriginId { get; set; }
        public int RevIndex { get; set; }
        public DateTime Time { get; set; }

        public int CompareTo(RevisionReference other)
        {
            int result = this.Time.CompareTo(other.Time);
            if (result != 0) return result;

            result = this.OriginId.CompareTo(other.OriginId);
            if (result != 0) return result;

            return this.RevIndex.CompareTo(other.RevIndex);
        }

        public bool Equals(RevisionReference other)
        {
            return this.OriginId.Equals(other.OriginId, StringComparison.InvariantCultureIgnoreCase) && this.RevIndex == other.RevIndex;
        }

        public override bool Equals(object obj)
        {
            var other = obj as RevisionReference;
            if (other == null) return false;
            return this.Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + OriginId.GetHashCode();
                hash = hash * 23 + RevIndex.GetHashCode();
                return hash;
            }
        }
    }
}
