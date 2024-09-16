using AutoFixture.AutoNSubstitute;
using AutoFixture;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using JiraExport;

namespace Migration.Jira_Export.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class ExportIssuesSummaryTests
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
        public void When_calling_get_report_string_with_no_unmapped_resources_Then_empty_string_is_returned()
        {
            ExportIssuesSummary sut = new ExportIssuesSummary();
            Assert.That(() => sut.GetReportString(), Is.Empty);
        }

        [Test]
        public void When_calling_get_report_string_with_unmapped_issue_type_Then_the_expected_substring_is_found()
        {
            string issueType = Guid.NewGuid().ToString();

            ExportIssuesSummary sut = new ExportIssuesSummary();

            sut.AddUnmappedIssueType(issueType);

            Assert.That(() => sut.GetReportString(), Contains.Substring($"- {issueType}"));
        }

        [Test]
        public void When_calling_get_report_string_with_unmapped_issue_state_Then_the_expected_substring_is_found()
        {
            string issueType = Guid.NewGuid().ToString();
            string issueState = Guid.NewGuid().ToString();

            ExportIssuesSummary sut = new ExportIssuesSummary();

            sut.AddUnmappedIssueState(issueType, issueState);

            Assert.That(() => sut.GetReportString(), Contains.Substring($"- {issueType}"));
            Assert.That(() => sut.GetReportString(), Contains.Substring($"  - {issueState}"));
        }

        [Test]
        public void When_calling_get_report_string_with_unmapped_user_Then_the_expected_substring_is_found()
        {
            string username = Guid.NewGuid().ToString();

            ExportIssuesSummary sut = new ExportIssuesSummary();

            sut.AddUnmappedUser(username);

            Assert.That(() => sut.GetReportString(), Contains.Substring($"- {username}"));
        }
    }
}
