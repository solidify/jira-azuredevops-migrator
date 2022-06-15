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
            DateTime datetime1 = new DateTime();
            TimeSpan timespan = TimeSpan.FromMilliseconds(50);

            Assert.AreEqual(datetime1+timespan, RevisionUtility.NextValidDeltaRev(datetime1));
        }

        [Test]
        public void When_calling_nextvaliddeltarev_with_next_more_than_current_Then_the_expeced_result_is_returned()
        {
            DateTime datetime1 = new DateTime();
            TimeSpan timespan = TimeSpan.FromMilliseconds(60);
            DateTime datetime2 = datetime1 + timespan;

            Assert.AreEqual(datetime1 + TimeSpan.FromMilliseconds(50), RevisionUtility.NextValidDeltaRev(datetime1, datetime2));
        }

        // ReplaceHtmlElements
        // HasAnyByRefName
    }
}