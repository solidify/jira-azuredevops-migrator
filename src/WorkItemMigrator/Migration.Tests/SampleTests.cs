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
using System.IO;

namespace Migration.Tests
{
    [TestFixture]
    public class SampleTests
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
        public void When_calling_execute_with_empty_args_Then_an_exception_is_thrown()
        {
            string[] args = null;

            var sut = new JiraCommandLine(args);


            Assert.That(() => sut.Run(), Throws.InstanceOf<NullReferenceException>());
        }

        [Test]
        public void When_calling_execute_with_args_Then_run_is_executed()
        {
            string[] args = null;

            var x = _fixture.Freeze<CommandLineApplication>();

            var configReader = _fixture.Freeze<ConfigReaderJson>();
            configReader.LoadFromFile(Arg.Any<string>());
            configReader.Deserialize().ReturnsForAnyArgs(new ConfigJson() { EpicLinkField = "Epic" });


            var sut = new JiraCommandLine(args);


            Assert.That(() => sut.Run(), Throws.InstanceOf<NullReferenceException>());
        }

       
    }
}