using Newtonsoft.Json;

namespace Migration.WIContract
{
    public class WiLink
    {
        public ReferenceChangeType Change { get; set; }

        [JsonIgnore]
        public string SourceOriginId { get; set; }

        public string TargetOriginId { get; set; }

        [JsonIgnore]
        public int SourceWiId { get; set; }

        public int TargetWiId { get; set; }

        public string WiType { get; set; }

        public override string ToString()
        {
            return $"[{Change}] {SourceOriginId}/{SourceWiId}->{TargetOriginId}/{TargetWiId} [{WiType}]";
        }
    }
}