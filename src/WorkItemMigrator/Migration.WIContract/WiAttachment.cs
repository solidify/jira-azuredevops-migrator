using System.IO;

namespace Migration.WIContract
{
    public class WiAttachment
    {
        public ReferenceChangeType Change { get; set; }
        public string FileName { get; private set; }
        public string Comment { get; set; }
        public string AttOriginId { get; set; }

        private string filePath;
        public string FilePath
        {
            get
            {
                return filePath;
            }
            set
            {
                filePath = value;
                FileName = Path.GetFileName(value);
            }
        }
        public override string ToString()
        {
            return $"[{Change}] {AttOriginId}/{FileName}";
        }
    }
}
