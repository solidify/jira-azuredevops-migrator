using NUnit.Framework;

using AutoFixture.AutoNSubstitute;
using AutoFixture;
using System.Collections.Generic;
using System;
using Migration.WIContract;

namespace Migration.Common.Tests
{
    [TestFixture]
    public class RevisionUtilityTests
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
        public void When_calling_nextvaliddeltarev_with_one_param_Then_the_expected_result_is_returned()
        {
            DateTime datetime = new DateTime();

            DateTime expected = datetime + TimeSpan.FromMilliseconds(50);
            DateTime actual = RevisionUtility.NextValidDeltaRev(datetime);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void When_calling_nextvaliddeltarev_with_next_more_than_current_Then_the_expected_result_is_returned()
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

        [Test]
        public void When_calling_hasanybyrefname_when_list_is_null_Then_false_is_returned()
        {
            List<WiField> list = null;

            bool expected = false;
            bool actual = RevisionUtility.HasAnyByRefName(list, "name");

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void When_calling_hasanybyrefname_when_list_is_empty_Then_false_is_returned()
        {
            List<WiField> list = new List<WiField>();

            bool expected = false;
            bool actual = RevisionUtility.HasAnyByRefName(list, "name");

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void When_calling_hasanybyrefname_when_list_contains_matching_refname_Then_true_is_returned()
        {
            WiField field = new WiField();
            field.ReferenceName = "name";
            List<WiField> list = new List<WiField>();
            list.Add(field);

            bool expected = true;
            bool actual = RevisionUtility.HasAnyByRefName(list, "name");

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void When_calling_hasanybyrefname_when_list_does_not_contain_matching_refname_Then_false_is_returned()
        {
            WiField field = new WiField();
            field.ReferenceName = "anothername";
            List<WiField> list = new List<WiField>();
            list.Add(field);

            bool expected = false;
            bool actual = RevisionUtility.HasAnyByRefName(list, "name");

            Assert.AreEqual(expected, actual);
        }
    }
}