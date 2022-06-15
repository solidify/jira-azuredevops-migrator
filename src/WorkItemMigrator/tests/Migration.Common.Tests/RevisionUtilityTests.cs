using NUnit.Framework;

using AutoFixture.AutoNSubstitute;
using AutoFixture;
using System.Collections.Generic;
using NSubstitute;
using System.IO.Abstractions;
using System;

namespace Migration.Common.Tests
{
    [TestFixture]
    public class RevisionUtilityTests
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
        public void When_calling_nextvaliddeltarev_with_one_param_Then_the_expeced_result_is_returned()
        {
            DateTime datetime = new DateTime();

            DateTime expected = datetime + TimeSpan.FromMilliseconds(50);
            DateTime actual = RevisionUtility.NextValidDeltaRev(datetime);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void When_calling_nextvaliddeltarev_with_next_more_than_current_Then_the_expeced_result_is_returned()
        {
            DateTime datetime1 = new DateTime();
            DateTime datetime2 = datetime1 + TimeSpan.FromMilliseconds(60);

            DateTime expected = datetime1 + TimeSpan.FromMilliseconds(50);
            DateTime actual = RevisionUtility.NextValidDeltaRev(datetime1, datetime2);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void When_calling_replacehtmlelements_Then_the_expected_result_is_returned()
        {
            string expected = "html";
            string actual = RevisionUtility.ReplaceHtmlElements("html");

            Assert.AreEqual(expected, actual);
        }
        // ReplaceHtmlElements
        // HasAnyByRefName
    }
}