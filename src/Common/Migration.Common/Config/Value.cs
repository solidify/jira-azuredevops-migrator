using Newtonsoft.Json;

namespace Migration.Common.Config
{
    public class Value
    {
        [JsonProperty("target")]
        public string Target { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }
    }
}