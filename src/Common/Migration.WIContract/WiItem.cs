using System.Collections.Generic;

namespace Migration.WIContract
{
    public class WiItem
    {
        public string Type { get; set; }
        public string OriginId { get; set; }
        public int WiId { get; set; } = -1;
        public List<WiRevision> Revisions { get; set; }

        public override string ToString()
        {
            return $"[{Type}]{OriginId}/{WiId}";
        }
    }
}
