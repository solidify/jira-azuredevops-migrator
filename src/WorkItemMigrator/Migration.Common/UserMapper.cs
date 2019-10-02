using System;
using System.IO;
using System.Collections.Generic;
using Migration.Common.Log;

namespace Migration.Common
{
    internal class UserMapper
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

                        internalUserMapping.Add(jiraUser, wiUser);
                    }
                }
            }

            return internalUserMapping;
        }
    }
}
