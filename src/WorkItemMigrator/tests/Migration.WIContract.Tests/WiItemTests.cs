using AutoFixture;
using AutoFixture.AutoNSubstitute;
using NUnit.Framework;
using System.Diagnostics.CodeAnalysis;

namespace Migration.WIContract.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class WiItemTests
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
            WiItem sut = new WiItem
            {
                Type = "type",
                OriginId = "originId",
                WiId = 1
            };

            string expectedToString = $"[{sut.Type}]{sut.OriginId}/{sut.WiId}";

            Assert.That(() => sut.ToString(), Is.EqualTo(expectedToString));

        }
    }
}