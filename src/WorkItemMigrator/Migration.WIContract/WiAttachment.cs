using System.IO;
using System;

namespace Migration.WIContract
{
    public class WiAttachment
    {
        public ReferenceChangeType Change { get; set; }
        public string FilePath { get; set; }
        public string Comment { get; set; }
        public Guid AttOriginId { get; set; }

        public override string ToString()
        {
            return $"[{Change.ToString()}] {AttOriginId}/{Path.GetFileName(FilePath)}";
        }
    }
}