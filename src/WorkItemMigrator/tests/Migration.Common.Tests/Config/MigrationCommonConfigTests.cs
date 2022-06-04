using NUnit.Framework;

using AutoFixture.AutoNSubstitute;
using AutoFixture;
using System.Collections.Generic;
using NSubstitute;
using Common.Config;
using Newtonsoft.Json;
using Migration.Common.Config;

namespace Migration.Common.Tests
{
    [TestFixture]
    public class MigrationCommonConfigTests
    {
        // use auto fixiture to help mock and instantiate with dummy data with nsubsitute. 
        private Fixture _fixture;

        [SetUp]
        public void Setup()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoNSubstituteCustomization() { });
        }

        [Test]
        public void When_parsing_settings_from_json_Then_all_fields_are_presenty()
        {
            //Assign
            string SourceProject = "SourceProject";
            string TargetProject = "TargetProject";
            string Query = "Query";
            string Workspace = "Workspace";
            string EpicLinkField = "EpicLinkField";
            string SprintField = "SprintField";
            int DownloadOptions = 1;
            int BatchSize = 1;
            string LogLevel = "LogLevel";
            string AttachmentsFolder = "AttachmentsFolder";
            string UserMappingFile = "UserMappingFile";
            string BaseAreaPath = "BaseAreaPath";
            string BaseIterationPath = "BaseIterationPath";
            bool IgnoreFailedLinks = true;
            string ProcessTemplate = "ProcessTemplate";
            string[] RenderedFields = new string[0];
            bool UsingJiraCloud = true;

            FieldMap fieldMap = new FieldMap();
            TypeMap typeMap = new TypeMap();
            LinkMap linkMap = new LinkMap();
            List<CharReplaceRule> CharReplaceMap = new List<CharReplaceRule>();

            string jsonString = "{"
                + string.Format("\"source-project\": \"{0}\",", SourceProject)
                + string.Format("\"target-project\": \"{0}\",", TargetProject)
                + string.Format("\"query\": \"{0}\",", Query)
                + string.Format("\"workspace\": \"{0}\",", Workspace)
                + string.Format("\"epic-link-field\": \"{0}\",", EpicLinkField)
                + string.Format("\"sprint-field\": \"{0}\",", SprintField)
                + string.Format("\"download-options\": \"{0}\",", DownloadOptions)
                + string.Format("\"batch-size\": \"{0}\",", BatchSize)
                + string.Format("\"log-level\": \"{0}\",", LogLevel)
                + string.Format("\"attachment-folder\": \"{0}\",", AttachmentsFolder)
                + string.Format("\"user-mapping-file\": \"{0}\",", UserMappingFile)
                + string.Format("\"base-area-path\": \"{0}\",", BaseAreaPath)
                + string.Format("\"base-iteration-path\": \"{0}\",", BaseIterationPath)
                + string.Format("\"ignore-failed-links\": \"{0}\",", IgnoreFailedLinks)
                + string.Format("\"field-map\": {0},", JsonConvert.SerializeObject(fieldMap))
                + string.Format("\"process-template\": \"{0}\",", ProcessTemplate)
                + string.Format("\"type-map\": {0},", JsonConvert.SerializeObject(typeMap))
                + string.Format("\"link-map\": {0},", JsonConvert.SerializeObject(linkMap))
                + string.Format("\"rendered-fields\": {0},", JsonConvert.SerializeObject(RenderedFields))
                + string.Format("\"using-jira-cloud\": \"{0}\",", UsingJiraCloud)
                + string.Format("\"sprint-char-replace-map\": {0}", JsonConvert.SerializeObject(CharReplaceMap))
                + "}";


            ConfigJson configJson = JsonConvert.DeserializeObject<ConfigJson>(jsonString);

            Assert.That(configJson.SourceProject, Is.EqualTo(SourceProject));
            Assert.That(configJson.TargetProject, Is.EqualTo(TargetProject));
            Assert.That(configJson.Query, Is.EqualTo(Query));
            Assert.That(configJson.Workspace, Is.EqualTo(Workspace));
            Assert.That(configJson.EpicLinkField, Is.EqualTo(EpicLinkField));
            Assert.That(configJson.SprintField, Is.EqualTo(SprintField));
            Assert.That(configJson.DownloadOptions, Is.EqualTo(DownloadOptions));
            Assert.That(configJson.BatchSize, Is.EqualTo(BatchSize));
            Assert.That(configJson.LogLevel, Is.EqualTo(LogLevel));
            Assert.That(configJson.AttachmentsFolder, Is.EqualTo(AttachmentsFolder));
            Assert.That(configJson.UserMappingFile, Is.EqualTo(UserMappingFile));
            Assert.That(configJson.BaseAreaPath, Is.EqualTo(BaseAreaPath));
            Assert.That(configJson.BaseIterationPath, Is.EqualTo(BaseIterationPath));
            Assert.That(configJson.IgnoreFailedLinks, Is.EqualTo(IgnoreFailedLinks));
            AreEqualByJson(configJson.FieldMap, fieldMap);
            Assert.That(configJson.ProcessTemplate, Is.EqualTo(ProcessTemplate));
            AreEqualByJson(configJson.TypeMap, typeMap);
            AreEqualByJson(configJson.LinkMap, linkMap);
            Assert.That(configJson.RenderedFields, Is.EqualTo(RenderedFields));
            Assert.That(configJson.UsingJiraCloud, Is.EqualTo(UsingJiraCloud));
            AreEqualByJson(configJson.CharReplaceRuleMap, CharReplaceMap);
        }

        private static void AreEqualByJson(object expected, object actual)
        {
            var expectedJson = JsonConvert.SerializeObject(expected);
            var actualJson = JsonConvert.SerializeObject(actual);
            Assert.AreEqual(expectedJson, actualJson);
        }
    }
}