using System.Collections.Generic;
using System.IO;

using Migration.Common.Log;

namespace Migration.Common
{
    internal static class UserMapper
    {
        public static Dictionary<string, string> ParseUserMappings(string userMappingPath)
        {
            var internalUserMapping = new Dictionary<string, string>();

            if (!File.Exists(userMappingPath))
            {
                Logger.Log(LogLevel.Warning, $"User mapping file '{userMappingPath}' not found, ignoring mapping user identities.");
            }
            else
            {
                string[] userMappings = File.ReadAllLines(userMappingPath);
                foreach (var userMapping in userMappings)
                {
                    var userMappingParts = userMapping.Split('=');
                    if (userMappingParts.Length == 2)
                    {
                        string jiraUser = userMappingParts[0].Trim();
                        string wiUser = userMappingParts[1].Trim();

                        if (!internalUserMapping.ContainsKey(jiraUser))
                            internalUserMapping.Add(jiraUser, wiUser);
                        else
                            Logger.Log(LogLevel.Warning, $"Duplicate mapping found {jiraUser}={wiUser} in user mapping configuration file");
                    }
                }
            }

            return internalUserMapping;
        }
    }
}
