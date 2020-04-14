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

        [JsonProperty(PropertyName = "workspace", Required = Required.Always)]
        public string Workspace { get; set; }

        [JsonProperty(PropertyName = "epic-link-field")]
        public string EpicLinkField { get; set; } = "Epic Link";

        [JsonProperty(PropertyName = "sprint-field")]
        public string SprintField { get; set; } = "Sprint";

        [JsonProperty(PropertyName = "download-options")]
        public int DownloadOptions { get; set; } = 7;   // = All, see DownloadOptions

        [JsonProperty(PropertyName = "batch-size")]
        public int BatchSize { get; set; } = 20;

        [JsonProperty(PropertyName = "log-level")]
        public string LogLevel { get; set; } = "Debug";

        [JsonProperty(PropertyName = "attachment-folder", Required = Required.Always)]
        public string AttachmentsFolder { get; set; }

        [JsonProperty(PropertyName = "user-mapping-file", Required = Required.AllowNull)]
        public string UserMappingFile { get; set; }

        [JsonProperty(PropertyName = "base-area-path")]
        public string BaseAreaPath { get; set; } = "";

        [JsonProperty(PropertyName = "base-iteration-path")]
        public string BaseIterationPath { get; set; } = "";

        [JsonProperty(PropertyName = "ignore-failed-links")]
        public bool IgnoreFailedLinks { get; set; } = false;

        [JsonProperty(PropertyName = "field-map", Required = Required.Always)]
        public FieldMap FieldMap { get; set; }

        [JsonProperty(PropertyName = "process-template")]
        public string ProcessTemplate { get; set; } = "Scrum";

        [JsonProperty(PropertyName = "type-map", Required = Required.Always)]
        public TypeMap TypeMap { get; set; }

        [JsonProperty(PropertyName = "link-map", Required = Required.Always)]
        public LinkMap LinkMap { get; set; }

        [JsonProperty(PropertyName = "rendered-fields")]
        public string[] RenderedFields { get; set; } = new string[] { "description", "comment" };


        [JsonProperty(PropertyName = "using-jira-cloud")]
        public bool UsingJiraCloud { get; set; } = true;
    }
}