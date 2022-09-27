using NUnit.Framework;

using AutoFixture.AutoNSubstitute;
using AutoFixture;
using Common.Config;
using System.IO;
using System.Diagnostics.CodeAnalysis;

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
            ConfigJson config = new ConfigJson();
            config.AttachmentsFolder = "AttachmentsFolder";
            config.UserMappingFile = "UserMappingFile";
            config.Workspace = "C:\\Temp\\JiraExport\\";
            MigrationContext.Init("app", config, "debug", true, "");

            Assert.Multiple(() =>
            {
                Assert.That(MigrationContext.Instance.AttachmentsPath, Is.EqualTo(Path.Combine(config.Workspace, config.AttachmentsFolder)));
                Assert.That(MigrationContext.Instance.UserMappingPath, Is.EqualTo(Path.Combine(config.Workspace, config.UserMappingFile)));
            });

        }

    }
}


