using System;
using System.Collections.Generic;
using Migration.Common.Log;

namespace Migration.Common
{
    public class BaseMapper<TRevision> where TRevision : ISourceRevision
    {
        protected Dictionary<string, string> UserMapping { get; private set; }

        public BaseMapper(string userMappingPath)
        {
            UserMapping = UserMapper.ParseUserMappings(userMappingPath);
        }

        protected virtual string MapUser(string sourceUser)
        {
            if (sourceUser == null)
                return sourceUser;

            if (UserMapping.TryGetValue(sourceUser, out string wiUser))
            {
                return wiUser;
            }
            else if (UserMapping.TryGetValue("*", out string defaultUser))
            {
                Logger.Log(LogLevel.Warning, $"Could not find user '{sourceUser}' identity in user map. Using default identity '{defaultUser}'.");
                return defaultUser;
            }
            else
            {
                Logger.Log(LogLevel.Warning, $"Could not find user '{sourceUser}' identity in user map. Using original identity '{sourceUser}'.");
                UserMapping.Add(sourceUser, sourceUser);
                return sourceUser;
            }
        }

        protected FieldMapping<TRevision> MergeMapping(params FieldMapping<TRevision>[] mappings)
        {
            var merged = new FieldMapping<TRevision>();
            foreach (var mapping in mappings)
            {
                foreach (var m in mapping)
                    if (!merged.ContainsKey(m.Key))
                        merged[m.Key] = m.Value;
            }
            return merged;
        }

        protected string Crop(string value, int maxSize)
        {
            var max = Math.Min(value.Length, maxSize);
            return value.Substring(0, max);
        }
    }
}
