using Newtonsoft.Json;

namespace Migration.Common.Config
{
    public class Type
    {
        [JsonProperty("target", Required = Required.Always)]
        public string Target { get; set; }

        [JsonProperty("source", Required = Required.Always)]
        public string Source { get; set; }
    }
}