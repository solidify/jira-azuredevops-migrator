using NUnit.Framework;

using JiraExport;
using WorkItemImport;

using Microsoft.Extensions.CommandLineUtils;
using static System.Configuration.ConfigurationManager;

namespace Migration.Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            Assert.Pass();
        }

        [Test]
        public void JiraExportTest()
        {
            string[] argsExport = new string[] {
                "-u",
                "alexander.hjelm@solidify.dev",
                "-p",
                "XXXXXXXXXXXXXXXXXXXXXXXXX",
                "--url",
                "https://solidifydemo.atlassian.net",
                "--config",
                "C:\\dev\\jira-azuredevops-migrator\\src\\WorkItemMigrator\\Migration.Tests\\test-config-export.json"
            };
            var cmdExport = new JiraCommandLine(argsExport);
            cmdExport.Run();

            string[] argsImport = new string[] {
                "-u",
                "username",
                "-p",
                "password",
                "--url",
                "https://dev.azure.com/solidifydemo",
                "--config",
                "C:\\dev\\jira-azuredevops-migrator\\src\\WorkItemMigrator\\Migration.Tests\\test-config-export.json"
            };
            //var cmdImport = new ImportCommandLine(argsImport);
            //cmdImport.Run();
        }
    }
}

// cd C:\dev\jira-azuredevops-migrator\src\WorkItemMigrator\JiraExport\bin\Debug
// .\jira-export.exe -u username -p password --url https://dev.azure.com/alexanderhjelmsolidify --config C:\dev\jira-azuredevops-migrator\src\WorkItemMigrator\Migration.Tests\test-config-export.json