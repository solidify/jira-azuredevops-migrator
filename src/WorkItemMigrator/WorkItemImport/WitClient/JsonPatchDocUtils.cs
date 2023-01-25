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

            if (value == null)
            {
                throw new ArgumentException(nameof(value));
            }

            return new JsonPatchOperation()
            {
                Operation = op,
                Path = "/fields/" + key,
                Value = value
            };
        }
    }
}
