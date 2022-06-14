using NUnit.Framework;

using AutoFixture;
using AutoFixture.AutoNSubstitute;

namespace Migration.WIContract.Tests
{
    [TestFixture]
    public class WiFieldTests
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
        public void When_calling_tostring_Then_the_expected_string_value_is_returned()
        {
            WiField sut = new WiField();

            sut.ReferenceName = "referenceName";
            sut.Value = "objValue";

            string expectedToString = "[" + sut.ReferenceName + "]=" + sut.Value;

            Assert.That(() => sut.ToString(), Is.EqualTo(expectedToString));
        }
    }
}