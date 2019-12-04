using System;
using System.Diagnostics;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Semver;

namespace Migration.Common.Log
{
    public static class VersionInfo
    {
        public static string GetVersionInfo()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetEntryAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fvi.ProductVersion;
            return version;
        }

        public static string GetCopyrightInfo()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetEntryAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fvi.LegalCopyright;
        }

        public static void PrintInfoMessage(string app)
        {
            Console.WriteLine($"{app} v{GetVersionInfo()}");
            Console.WriteLine(GetCopyrightInfo());
            if (VersionInfo.NewerVersionExists(out string latestVersion))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Wow, there's a newer release out (v{latestVersion})! We recommend downloading it for latest features and fixes.");
                Console.ResetColor();
            }
        }

        private static bool NewerVersionExists(out string latestVersion)
        {
            var currentVersion = latestVersion = GetVersionInfo();

            try
            {
                latestVersion = GetLatestReleaseVersion();

                if (SemVersion.Parse(latestVersion) > SemVersion.Parse(currentVersion))
                    return true;
            }
            catch
            {
                return false;
            }

            return false;
        }

        private static string GetLatestReleaseVersion()
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Other");

                var response = httpClient.GetStringAsync(new Uri("https://api.github.com/repos/solidify/jira-azuredevops-migrator/releases/latest")).Result;

                JObject o = JObject.Parse(response);
                var ver = o.SelectToken("$.name").Value<string>();

                return ver.Replace("v", "");
            }
        }
    }
}