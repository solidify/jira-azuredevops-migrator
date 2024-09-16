using AutoFixture;
using AutoFixture.AutoNSubstitute;
using Common.Config;
using NUnit.Framework;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Migration.Common.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class MigrationContextTests
    {
        private Fixture _fixture;

        [SetUp]
        public void Setup()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoNSubstituteCustomization() { });
        }

        [Test]
        public void When_initializing_migration_context_Then_folder_paths_are_correct()
        {
            ConfigJson config = new ConfigJson
            {
                AttachmentsFolder = "AttachmentsFolder",
                UserMappingFile = "UserMappingFile",
                Workspace = "C:\\Temp\\JiraExport\\"
            };
            MigrationContext.Init("app", config, "debug", true, "");

            Assert.Multiple(() =>
            {
                Assert.That(MigrationContext.Instance.AttachmentsPath, Is.EqualTo(Path.Combine(config.Workspace, config.AttachmentsFolder)));
                Assert.That(MigrationContext.Instance.UserMappingPath, Is.EqualTo(Path.Combine(config.Workspace, config.UserMappingFile)));
            });

        }

    }
}


