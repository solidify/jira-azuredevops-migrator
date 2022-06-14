using NUnit.Framework;

using AutoFixture;
using AutoFixture.AutoNSubstitute;

namespace Migration.WIContract.Tests
{
    [TestFixture]
    public class WiItemTests
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
        public void When_calling_ToString_Then_the_expected_String_value_is_returned()
        {
            WiItem sut = new WiItem();

            sut.Type = "type";
            sut.OriginId = "originId";
            sut.WiId = 1;

            string expectedToString = "[" + sut.Type + "]"+ sut.OriginId +"/" + sut.WiId;

            Assert.That(() => sut.ToString(), Is.EqualTo(expectedToString));

        }
    }
}