using NUnit.Framework;

using JiraExport;
using WorkItemImport;

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
            string[] args = new string[] {
                "-u",
                "username",
                "-p",
                "password",
                "--url",
                "https://dev.azure.com/alexanderhjelmsolidify",
                "--config",
                "C:\\dev\\jira-azuredevops-migrator\\src\\WorkItemMigrator\\Migration.Tests\\test-config-export.json"
            };
            var cmdExport = new JiraCommandLine(args);
            cmdExport.Run();

            var cmdImport = new ImportCommandLine(args);
            cmdImport.Run();
        }
    }
}