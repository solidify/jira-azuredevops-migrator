using System;

namespace WorkItemImport
{
    public sealed class RevisionReference : IComparable<RevisionReference>, IEquatable<RevisionReference>
    {
        public string OriginId { get; set; }
        public int RevIndex { get; set; }
        public DateTime Time { get; set; }

        public int CompareTo(RevisionReference other)
        {
            int result = Time.CompareTo(other.Time);
            if (result != 0) return result;

            result = OriginId.CompareTo(other.OriginId);
            if (result != 0) return result;

            return RevIndex.CompareTo(other.RevIndex);
        }

        public bool Equals(RevisionReference other)
        {
            return OriginId.Equals(other.OriginId, StringComparison.InvariantCultureIgnoreCase) && RevIndex == other.RevIndex;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is RevisionReference other)) return false;
            return Equals(other);
        }

        public static bool operator ==(RevisionReference left, RevisionReference right)
        {
            if (left is null)
            {
                return right is null;
            }
            return left.Equals(right);
        }

        public static bool operator >(RevisionReference left, RevisionReference right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator >=(RevisionReference left, RevisionReference right)
        {
            return left.CompareTo(right) >= 0;
        }

        public static bool operator <=(RevisionReference left, RevisionReference right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator <(RevisionReference left, RevisionReference right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator !=(RevisionReference left, RevisionReference right)
        {
            return !(left == right);
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