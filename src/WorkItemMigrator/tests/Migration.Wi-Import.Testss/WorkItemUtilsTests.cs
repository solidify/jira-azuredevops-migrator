using NUnit.Framework;

using AutoFixture.AutoNSubstitute;
using AutoFixture;
using System;
using WorkItemImport;
using Migration.WIContract;
//using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace Migration.Wi_Import.Testss
{
    [TestFixture]
    public class WorkItemUtilsTests
    {
        // use auto fixiture to help mock and instantiate with dummy data with nsubsitute. 
        private Fixture _fixture;

        [SetUp]
        public void Setup()
        {
            _fixture = new Fixture();
            _fixture.Customize(new AutoNSubstituteCustomization() { });
        }

        /*
        public static void CorrectImagePath(WorkItem wi, WiItem wiItem, WiRevision rev, ref string textField, ref bool isUpdated, IsAttachmentMigratedDelegate<string, int, bool> isAttachmentMigratedDelegate)
        {
            foreach (var att in wiItem.Revisions.SelectMany(r => r.Attachments.Where(a => a.Change == ReferenceChangeType.Added)))
            {
                var fileName = att.FilePath.Split('\\')?.Last() ?? string.Empty;
                if (textField.Contains(fileName))
                {
                    var tfsAtt = IdentifyAttachment(att, wi, isAttachmentMigratedDelegate);

                    if (tfsAtt != null)
                    {
                        string imageSrcPattern = $"src.*?=.*?\"([^\"])(?=.*{att.AttOriginId}).*?\"";
                        textField = Regex.Replace(textField, imageSrcPattern, $"src=\"{tfsAtt.Uri.AbsoluteUri}\"");
                        isUpdated = true;
                    }
                    else
                        Logger.Log(LogLevel.Warning, $"Attachment '{att.ToString()}' referenced in text but is missing from work item {wiItem.OriginId}/{wi.Id}.");
                }
            }
            if (isUpdated)
            {
                DateTime changedDate;
                if (wiItem.Revisions.Count > rev.Index + 1)
                    changedDate = RevisionUtility.NextValidDeltaRev(rev.Time, wiItem.Revisions[rev.Index + 1].Time);
                else
                    changedDate = RevisionUtility.NextValidDeltaRev(rev.Time);

                wi.Fields[WiFieldReference.ChangedDate].Value = changedDate;
                wi.Fields[WiFieldReference.ChangedBy].Value = rev.Author;
            }
        }
        */

        private bool MockedIsAttachmentMigratedDelegate(string _attOriginId, out int attWiId)
        {
            attWiId = 1;
            return true;
        }

        [Test]
        public void When_calling_correct_image_path_with_empty_args_Then_an_exception_is_thrown()
        {
            WorkItem wi = new WorkItem();
            WiItem wiItem = new WiItem();
            WiRevision rev = new WiRevision();
            string textField = "";

            WorkItemUtils.CorrectImagePath(wi, wiItem, rev, textField, true, MockedIsAttachmentMigratedDelegate);

            WorkItemUtils.EnsureAuthorFields(null);

            Assert.That(
                () => WorkItemUtils.CorrectImagePath(wi, wiItem, rev, textField, true, MockedIsAttachmentMigratedDelegate),
                Throws.InstanceOf<NullReferenceException>());
        }

        [Test]
        public void When_calling_execute_with_args_Then_run_is_executed()
        {

            string[] args = new string[] {
                "--token",
                "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
                "--url",
                "https://dev.azure.com/solidifydemo",
                "--config",
                "C:\\dev\\jira-azuredevops-migrator\\src\\WorkItemMigrator\\Migration.Tests\\test-config-export.json"
            };

            var sut = new ImportCommandLine(args);

            Assert.That(() => sut.Run(), !Throws.InstanceOf<Exception>());


        }
    }
}