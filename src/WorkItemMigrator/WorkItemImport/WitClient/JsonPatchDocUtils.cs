using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using System;
using System.Web;

namespace WorkItemImport.WitClient
{
    public static class JsonPatchDocUtils
    {
        public class PatchOperationValue
        {
            public string Rel { get; set; }
            public string Url { get; set; }
            public Attributes Attributes { get; set; }
        }

        public class Attributes
        {
            public string Name { get; set; }
        }

        public static JsonPatchOperation CreateJsonFieldPatchOp(Operation op, string key, object value)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException(nameof(key));
            }

            return new JsonPatchOperation()
            {
                Operation = op,
                Path = "/fields/" + key,
                Value = value
            };
        }

        public static JsonPatchOperation CreateJsonArtifactLinkPatchOp(
            Operation op,
            string projectId,
            string repositoryId,
            string developmentLinkId,
            string type
        )
        {
            if (string.IsNullOrEmpty(developmentLinkId))
            {
                throw new ArgumentException(nameof(developmentLinkId));
            }

            if (string.IsNullOrEmpty(projectId))
            {
                throw new ArgumentException(nameof(projectId));
            }

            if (string.IsNullOrEmpty(repositoryId))
            {
                throw new ArgumentException(nameof(repositoryId));
            }

            string url;
            string nameAttribute;
            if (type == "Commit")
            {
                url = $"vstfs:///Git/Commit/{projectId}%2F{repositoryId}%2F{developmentLinkId}";
                nameAttribute = "Fixed in Commit";
            }
            else
            {
                throw new ArgumentException(nameof(type));
            }

            return new JsonPatchOperation()
            {
                Operation = op,
                Path = "/relations/-",
                Value = new PatchOperationValue
                {
                    Rel = "ArtifactLink",
                    Url = url,
                    Attributes = new Attributes
                    {
                        Name = nameAttribute
                    }
                }
            };
        }
    }
}
