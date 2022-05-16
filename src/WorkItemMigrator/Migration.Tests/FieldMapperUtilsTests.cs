using NUnit.Framework;

using JiraExport;
using WorkItemImport;
using AutoFixture.AutoNSubstitute;
using AutoFixture;
using System;
using Microsoft.Extensions.CommandLineUtils;
using Migration.Common.Config;
using NSubstitute;
using Common.Config;

namespace Migration.Tests
{
    [TestFixture]
    public class FieldMapperUtilsTests
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
        public void When_calling_map_remaining_work_with_valid_args_Then_output_is_correct()
        {
            object output = FieldMapperUtils.MapRemainingWork("36000");
            Assert.AreEqual (output, 10);
        }
       
    }
}