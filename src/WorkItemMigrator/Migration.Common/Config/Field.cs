using Newtonsoft.Json;

namespace Migration.Common.Config
{
    public class Field
    {
        [JsonProperty("target", Required = Required.Always)]
        public string Target { get; set; }

        [JsonProperty("source", Required = Required.Always)]
        public string Source { get; set; }

        [JsonProperty("for")]
        public string For { get; set; } = "All";

        [JsonProperty("not-for")]
        public string NotFor { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; } = "string";
        
        [JsonProperty("mapper")]
        public string Mapper { get; set; }

        [JsonProperty("process")]
        public string Process { get; set; } = "Common";
    }
}