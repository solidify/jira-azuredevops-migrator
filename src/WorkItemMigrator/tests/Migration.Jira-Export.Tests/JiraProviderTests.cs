using NUnit.Framework;

using JiraExport;
using AutoFixture.AutoNSubstitute;
using AutoFixture;
using Migration.WIContract;
using Common.Config;
using System.Collections.Generic;
using Migration.Common;
using Migration.Common.Config;
using Newtonsoft.Json.Linq;
using NSubstitute;
using Atlassian.Jira.Remote;
using Atlassian.Jira;
using RestSharp;
using NSubstitute.ReturnsExtensions;
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
            string customFieldId = "customfield_00002";
            string propertyName = "Sprint";
            
            var apiResponse = JArray.Parse(
                $"[{{ 'id': 'customfield_00001', 'name': 'Story'}}, " +
                $"{{ 'id': '{customFieldId}', 'name': '{propertyName}'}}]");
            
            var restClientMock = _fixture.Create<IJiraRestClient>();
            restClientMock.ExecuteRequestAsync(Method.GET, Arg.Any<string>()).Returns(apiResponse);

            JiraProvider sut = new JiraProvider(restClientMock, _fixture.Create<IIssueLinkService>(),
                _fixture.Create<IIssueFieldService>(), _fixture.Create<IIssueService>(), _fixture.Create<IJiraUserService>());

            //Act
            var id = sut.GetCustomId(propertyName);

            //Assert
            Assert.AreEqual(customFieldId, id);
        }

        [Test]
        public void When_calling_getcustomid_the_key_matches_Then_the_expected_result_is_returned()
        {
            //Arrange
            string customFieldId = "customfield_00002";
            string propertyName = "Sprint";

            var apiResponse = JArray.Parse(
                $"[{{ 'id': 'customfield_00001', 'key': 'Story'}}, " +
                $"{{ 'id': '{customFieldId}', 'key': '{propertyName}'}}]");

            var restClientMock = _fixture.Create<IJiraRestClient>();
            restClientMock.ExecuteRequestAsync(Method.GET, Arg.Any<string>()).Returns(apiResponse);

            JiraProvider sut = new JiraProvider(restClientMock, _fixture.Create<IIssueLinkService>(),
                _fixture.Create<IIssueFieldService>(), _fixture.Create<IIssueService>(), _fixture.Create<IJiraUserService>());

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

            var restClientMock = _fixture.Create<IJiraRestClient>();
            restClientMock.ExecuteRequestAsync(Method.GET, Arg.Any<string>()).Returns(apiResponse);

            JiraProvider sut = new JiraProvider(restClientMock, _fixture.Create<IIssueLinkService>(),
                _fixture.Create<IIssueFieldService>(), _fixture.Create<IIssueService>(), _fixture.Create<IJiraUserService>());

            //Act
            var id = sut.GetCustomId(propertyName);

            //Assert
            Assert.AreEqual(null, id);
        }

        [Test]
        public void When_calling_getcustomid_multiple_times_Then_the_api_is_called_once()
        {
            //Arrange
            string customFieldId1 = "customfield_00001";
            string customFieldId2 = "customfield_00002";
            string propertyName1 = "Story";
            string propertyName2 = "Sprint";

            var apiResponse = JArray.Parse(
                $"[{{ 'id': '{customFieldId1}', 'key': '{propertyName1}'}}, " +
                $"{{ 'id': '{customFieldId2}', 'key': '{propertyName2}'}}]");

            var restClientMock = _fixture.Create<IJiraRestClient>();
            restClientMock.ExecuteRequestAsync(Method.GET, Arg.Any<string>()).Returns(apiResponse);

            JiraProvider sut = new JiraProvider(restClientMock, _fixture.Create<IIssueLinkService>(),
                _fixture.Create<IIssueFieldService>(), _fixture.Create<IIssueService>(), _fixture.Create<IJiraUserService>());

            //Act
            var actualId1 = sut.GetCustomId(propertyName1);
            var actualId2 = sut.GetCustomId(propertyName2);

            //Assert
            Assert.Multiple(() =>
            {
                Assert.AreEqual(restClientMock.ReceivedCalls().Count(), 1);
                Assert.AreEqual(customFieldId1, actualId1);
                Assert.AreEqual(customFieldId2, actualId2);
            });            
        }
    }
}