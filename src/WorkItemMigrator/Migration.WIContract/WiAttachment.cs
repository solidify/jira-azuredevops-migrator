using System.IO;

namespace Migration.WIContract
{
    public class WiAttachment
    {
        public ReferenceChangeType Change { get; set; }
        public string FilePath { get; set; }
        public string Comment { get; set; }
        public string AttOriginId { get; set; }

        public override string ToString()
        {
            return $"[{Change.ToString()}] {AttOriginId}/{Path.GetFileName(FilePath)}";
        }
    }
}