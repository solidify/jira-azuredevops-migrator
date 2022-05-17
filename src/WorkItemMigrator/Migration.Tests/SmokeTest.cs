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
        public void DummyTest()
        {
            Assert.Pass();
        }

        [Test, Order(1)]
        public void JiraExportTest()
        {
            string[] argsExport = new string[] {
                "-u",
                "alexander.hjelm@solidify.dev",
                "-p",
                "WhDrzDMUevV8NqqANpXT446E",
                "--url",
                "https://solidifydemo.atlassian.net",
                "--config",
                "C:\\dev\\jira-azuredevops-migrator\\src\\WorkItemMigrator\\Migration.Tests\\test-config-export.json"
            };
            JiraCommandLine cmdExport = new JiraCommandLine(argsExport);
            cmdExport.Run();
        }

        [Test, Order(2)]
        public void AzDOImportTest()
        {
            string[] argsImport = new string[] {
                "--token",
                "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
                "--url",
                "https://dev.azure.com/solidifydemo",
                "--config",
                "C:\\dev\\jira-azuredevops-migrator\\src\\WorkItemMigrator\\Migration.Tests\\test-config-export.json"
            };
            ImportCommandLine cmdImport = new ImportCommandLine(argsImport);
            cmdImport.Run();
        }
    }
}

// cd C:\dev\jira-azuredevops-migrator\src\WorkItemMigrator\JiraExport\bin\Debug
// .\jira-export.exe -u username -p password --url https://dev.azure.com/alexanderhjelmsolidify --config C:\dev\jira-azuredevops-migrator\src\WorkItemMigrator\Migration.Tests\test-config-export.json