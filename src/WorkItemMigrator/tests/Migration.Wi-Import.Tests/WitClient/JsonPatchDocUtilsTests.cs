using NUnit.Framework;

using AutoFixture.AutoNSubstitute;
using AutoFixture;
using System;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Microsoft.VisualStudio.Services.WebApi.Patch;

using System.Diagnostics.CodeAnalysis;
using WorkItemImport.WitClient;

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
        public void When_calling_create_json_field_patch_op_Then_a_correct_op_is_returned()
        {
            JsonPatchOperation jsonPatchOp = JsonPatchDocUtils.CreateJsonFieldPatchOp(Operation.Add, "key", "value");

            Assert.Multiple(() =>
            {
                Assert.That(jsonPatchOp.Operation == Operation.Add);
                Assert.That(jsonPatchOp.Path == "/fields/key");
                Assert.That(jsonPatchOp.Value.ToString() == "value");
            });

        }
    }
}