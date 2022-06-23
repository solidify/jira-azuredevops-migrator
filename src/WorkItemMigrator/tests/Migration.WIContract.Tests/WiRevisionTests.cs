using NUnit.Framework;

using AutoFixture;
using AutoFixture.AutoNSubstitute;
using System.Collections.Generic;

namespace Migration.WIContract.Tests
{
    [TestFixture]
    public class WiRevisionTests
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
        public void When_calling_ToString_Then_the_expected_String_value_is_returned()
        {
            WiRevision sut = new WiRevision();

            sut.ParentOriginId = "parentOriginId";
            sut.Index = 1;

            string expectedToString = "'" + sut.ParentOriginId + "', rev " + sut.Index;

            Assert.That(() => sut.ToString(), Is.EqualTo(expectedToString));
        }
    }
}