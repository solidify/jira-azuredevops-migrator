using NUnit.Framework;

using AutoFixture.AutoNSubstitute;
using AutoFixture;
using System.Collections.Generic;
using NSubstitute;
using System.IO.Abstractions;
using Newtonsoft.Json.Linq;

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
        public void When_generating_user_map_Then_map_is_correct()
        {
            string[] userMapLines = { "a@jira.com=a@azdo.com", "b@jira.com=b@azdo.com" };
            Dictionary<string, string> generatedUserMap = UserMapper.ParseUserMappings(userMapLines);

            foreach(string line in userMapLines)
            {
                string[] splitLine = line.Split("=");
                string source = splitLine[0];
                string target = splitLine[1];

                Assert.Contains(source, generatedUserMap.Keys);
                Assert.AreEqual(target, generatedUserMap[source]);
            }
        }

        [Test]
        public void When_calling_ParseUserMappings_with_non_exisiting_file_Return_empty_Dictionary()
        {
            //Assign

            var fileSystem = _fixture.Freeze<IFileSystem>();
            fileSystem.File.Exists(Arg.Any<string>()).Returns(true);

            var expected = new Dictionary<string, string>();

            //Act
            var actualResult = UserMapper.ParseUserMappings("");

            //Assert
            Assert.That(actualResult.Count, Is.EqualTo(expected.Count));
        }

    }
}