using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using System;

namespace WorkItemImport.WitClient
{
    public static class JsonPatchDocUtils
    {
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

        public static JsonPatchOperation CreateJsonArtifactLinkPatchOp(Operation op, string project, string repository, string commitId)
        {
            if (string.IsNullOrEmpty(commitId))
            {
                throw new ArgumentException(nameof(commitId));
            }

            if (string.IsNullOrEmpty(project))
            {
                throw new ArgumentException(nameof(project));
            }

            if (string.IsNullOrEmpty(repository))
            {
                throw new ArgumentException(nameof(repository));
            }

            return new JsonPatchOperation()
            {
                Operation = op,
                Path = "/relations/-",
                Value = new {
                    Rel = "ArtifactLink",
                    Url = $"vstfs:///Git/Commit/{project}/{repository}/{commitId}",
                    Attributes = new {
                        Name = "Fixed in Commit"
                    }
                }
            };
        }
    }
}
