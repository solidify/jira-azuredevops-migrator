using NUnit.Framework;

using JiraExport;
using AutoFixture.AutoNSubstitute;
using AutoFixture;
using Newtonsoft.Json.Linq;
using NSubstitute;
using RestSharp;
using System.Linq;

namespace Migration.Jira_Export.Tests
{
    [TestFixture]
    public class JiraProviderTests
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
        public void When_calling_getcustomid_Then_the_expected_result_is_returned()
        {
            //Arrange
            string customFieldId = _fixture.Create<string>();
            string propertyName = _fixture.Create<string>(); ;
            
            var apiResponse = JArray.Parse(
                $"[{{ 'id': 'customfield_00001', 'name': 'Story'}}, " +
                $"{{ 'id': '{customFieldId}', 'name': '{propertyName}'}}]");

            var jiraServiceMock = _fixture.Create<IJiraServiceWrapper>();
            jiraServiceMock.RestClient.ExecuteRequestAsync(Method.GET, Arg.Any<string>()).Returns(apiResponse);

            JiraProvider sut = new JiraProvider(jiraServiceMock);

            //Act
            var id = sut.GetCustomId(propertyName);

            //Assert
            Assert.AreEqual(customFieldId, id);
        }

        [Test]
        public void When_calling_getcustomid_the_key_matches_Then_the_expected_result_is_returned()
        {
            //Arrange
            string customFieldId = _fixture.Create<string>(); ;
            string propertyName = _fixture.Create<string>(); ;

            var apiResponse = JArray.Parse(
                $"[{{ 'id': 'customfield_00001', 'key': 'Story'}}, " +
                $"{{ 'id': '{customFieldId}', 'key': '{propertyName}'}}]");

            var jiraServiceMock = _fixture.Create<IJiraServiceWrapper>();
            jiraServiceMock.RestClient.ExecuteRequestAsync(Method.GET, Arg.Any<string>()).Returns(apiResponse);

            JiraProvider sut = new JiraProvider(jiraServiceMock);

            //Act
            var id = sut.GetCustomId(propertyName);

            //Assert
            Assert.AreEqual(customFieldId, id);
        }

        [Test]
        public void When_calling_getcustomid_and_nothing_matches_Then_null_is_returned()
        {
            //Arrange
            string propertyName = "does_not_exist";

            var apiResponse = JArray.Parse(
                $"[{{ 'id': 'customfield_00001', 'key': 'Story'}}, " +
                $"{{ 'id': 'customfield_00002', 'key': 'Sprint'}}]");

            var jiraServiceMock = _fixture.Create<IJiraServiceWrapper>();
            jiraServiceMock.RestClient.ExecuteRequestAsync(Method.GET, Arg.Any<string>()).Returns(apiResponse);

            JiraProvider sut = new JiraProvider(jiraServiceMock);

            //Act
            var id = sut.GetCustomId(propertyName);

            //Assert
            Assert.AreEqual(null, id);
        }

        [Test]
        public void When_calling_getcustomid_multiple_times_Then_the_api_is_called_once()
        {
            //Arrange
            string customFieldId1 = _fixture.Create<string>(); ;
            string customFieldId2 = _fixture.Create<string>(); ;
            string propertyName1 = _fixture.Create<string>(); ;
            string propertyName2 = _fixture.Create<string>(); ;

            var apiResponse = JArray.Parse(
                $"[{{ 'id': '{customFieldId1}', 'key': '{propertyName1}'}}, " +
                $"{{ 'id': '{customFieldId2}', 'key': '{propertyName2}'}}]");

            var jiraServiceMock = _fixture.Create<IJiraServiceWrapper>();
            jiraServiceMock.RestClient.ExecuteRequestAsync(Method.GET, Arg.Any<string>()).Returns(apiResponse);

            JiraProvider sut = new JiraProvider(jiraServiceMock);

            //Act
            var actualId1 = sut.GetCustomId(propertyName1);
            var actualId2 = sut.GetCustomId(propertyName2);

            //Assert
            Assert.Multiple(() =>
            {
                Assert.AreEqual(jiraServiceMock.RestClient.ReceivedCalls().Count(), 1);
                Assert.AreEqual(customFieldId1, actualId1);
                Assert.AreEqual(customFieldId2, actualId2);
            });            
        }
    }
}
