using AutoFixture;
using AutoFixture.AutoNSubstitute;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using NUnit.Framework;
using System;
using System.Diagnostics.CodeAnalysis;
using WorkItemImport.WitClient;
using static WorkItemImport.WitClient.JsonPatchDocUtils;

namespace Migration.Wi_Import.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class JsonPatchDocUtilsTests
    {
        // use auto fixture to help mock and instantiate with dummy data with nsubsitute. 
        private Fixture _fixture;

        [SetUp]
        public void Setup()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoNSubstituteCustomization() { });
        }

        [Test]
        public void When_calling_create_json_field_patch_op_with_empty_args_Then_an_exception_is_thrown()
        {
            Assert.That(
                () => JsonPatchDocUtils.CreateJsonFieldPatchOp(Operation.Add, null, null),
                Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void When_calling_create_json_artifact_link_field_patch_op_with_empty_args_Then_an_exception_is_thrown()
        {
            Assert.That(
                () => JsonPatchDocUtils.CreateJsonArtifactLinkPatchOp(Operation.Add, null, null, null, null),
                Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void When_calling_create_json_field_patch_op_Then_a_correct_op_is_returned()
        {
            string key = "key";
            string value = "value";
            JsonPatchOperation jsonPatchOp = JsonPatchDocUtils.CreateJsonFieldPatchOp(Operation.Add, key, value);

            Assert.Multiple(() =>
            {
                Assert.That(jsonPatchOp.Operation == Operation.Add);
                Assert.That(jsonPatchOp.Path == "/fields/" + key);
                Assert.That(jsonPatchOp.Value.ToString() == value);
            });
        }

        [Test]
        public void When_calling_create_json_artifact_link_field_patch_op_Then_a_correct_op_is_returned()
        {
            string projectId = Guid.NewGuid().ToString();
            string repositoryId = Guid.NewGuid().ToString();
            string commitId = Guid.NewGuid().ToString();
            JsonPatchOperation jsonPatchOp = JsonPatchDocUtils.CreateJsonArtifactLinkPatchOp(Operation.Add, projectId, repositoryId, commitId, "Commit");
            PatchOperationValue artifactLink = jsonPatchOp.Value as PatchOperationValue;

            Assert.Multiple(() =>
            {
                Assert.AreEqual(Operation.Add, jsonPatchOp.Operation);
                Assert.AreEqual("/relations/-", jsonPatchOp.Path);
                Assert.AreEqual("ArtifactLink", artifactLink.Rel);
                Assert.AreEqual($"vstfs:///Git/Commit/{projectId}%2F{repositoryId}%2F{commitId}", artifactLink.Url);
            });
        }
    }
}