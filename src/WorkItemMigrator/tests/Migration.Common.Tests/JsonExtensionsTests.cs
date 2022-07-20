using NUnit.Framework;

using AutoFixture.AutoNSubstitute;
using AutoFixture;
using System.Collections.Generic;
using NSubstitute;
using System.IO.Abstractions;
using Newtonsoft.Json.Linq;
using System;

namespace Migration.Common.Tests
{
    [TestFixture]
    public class JsonExtensionsTests
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
        public void When_getvalues_Then_the_expected_result_is_returned()
        {
            JObject jObject = JObject.Parse(@"{ name: 'My Name', emails: [ 'my@email.com', 'my2@email.com' ]}");

            var expected = jObject.SelectToken("emails", false);
            var actual = JsonExtensions.GetValues<JToken>(jObject, "emails");

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void When_getvalues_with_non_existent_field_Then_an_exception_is_thrown()
        {
            JObject jObject = JObject.Parse(@"{ name: 'My Name', emails: [ 'my@email.com', 'my2@email.com' ]}");
            Assert.Throws<NullReferenceException>(() => { JsonExtensions.GetValues<JToken>(jObject, "addresses"); });
        }

        [Test]
        public void When_getvalues_with_null_input_Then_an_exception_is_thrown()
        {
            Assert.Throws<ArgumentNullException>(() => { JsonExtensions.GetValues<JToken>(null, ""); });
        }

    }
}