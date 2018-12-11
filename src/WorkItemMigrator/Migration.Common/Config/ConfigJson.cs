using Migration.Common.Config;
using Newtonsoft.Json;

namespace Common.Config
{
    public class ConfigJson
    {
        [JsonProperty(PropertyName = "source-project", Required = Required.Always)]
        public string SourceProject { get; set; }

        [JsonProperty(PropertyName = "target-project", Required = Required.Always)]
        public string TargetProject { get; set; }

        [JsonProperty(PropertyName = "query", Required = Required.Always)]
        public string Query { get; set; }

        [JsonProperty(PropertyName ="workspace", Required = Required.Always)]
        public string Workspace { get; set; }

        [JsonProperty(PropertyName = "batch-size", Required = Required.Always)]
        public int BatchSize { get; set; }

        [JsonProperty(PropertyName = "log-level", Required = Required.Always)]
        public string LogLevel { get; set; }

        [JsonProperty(PropertyName = "attachment-folder", Required = Required.Always)]
        public string AttachmentsFolder { get; set; }

        [JsonProperty(PropertyName = "user-mapping-file", Required = Required.AllowNull)]
        public string UserMappingFile { get; set; }

        [JsonProperty(PropertyName = "base-area-path", Required = Required.AllowNull)]
        public string BaseAreaPath { get; set; }

        [JsonProperty(PropertyName = "base-iteration-path", Required = Required.AllowNull)]
        public string BaseIterationPath { get; set; }

        [JsonProperty(PropertyName = "ignore-failed-links", Required = Required.Always)]
        public bool IgnoreFailedLinks { get; set; }

        [JsonProperty(PropertyName = "field-map", Required = Required.Always)]
        public FieldMap FieldMap { get; set; }

        [JsonProperty(PropertyName = "process-template", Required = Required.Always)]
        public string ProcessTemplate { get; set; }

        [JsonProperty(PropertyName = "type-map", Required = Required.Always)]
        public TypeMap TypeMap { get; set; }

        [JsonProperty(PropertyName = "link-map", Required = Required.Always)]
        public LinkMap LinkMap { get; set; }
    }
}