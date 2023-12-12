using Migration.WIContract;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using WorkItemImport;


namespace Migration.Wi_Import.Tests
{
    [TestFixture]
    public class BoardColumnCollectorTests
    {
        private BoardColumnCollector _boardColumnCollector;

        [SetUp]
        public void Setup()
        {
            _boardColumnCollector = new BoardColumnCollector();
        }

        [Test]
        public void ProcessFields_WithFinalRevision_AddsLatestBoardColumnValueToRevisionFields()
        {
            // Arrange
            var executionItem1 = new ExecutionPlan.ExecutionItem
            {
                Revision = new WiRevision
                {
                    ParentOriginId = "1",
                    Fields = new List<WiField>
                    {
                        new WiField { ReferenceName = WiFieldReference.BoardColumn, Value = "To Do" }
                    }
                },
                OriginId = "1",
                IsFinal = false
            };            
            var executionItem2 = new ExecutionPlan.ExecutionItem
            {
                Revision = new WiRevision
                {
                    ParentOriginId = "1",
                    Fields = new List<WiField>
                    {
                        new WiField { ReferenceName = WiFieldReference.BoardColumn, Value = "Second Value" }
                    }
                },
                OriginId = "1",
                IsFinal = true
            };

            // Act
            _boardColumnCollector.ProcessFields(executionItem1);
            _boardColumnCollector.ProcessFields(executionItem2);

            // Assert
            var boardColumnField = executionItem2.Revision.Fields.FirstOrDefault(f => f.ReferenceName == WiFieldReference.BoardColumn);
            Assert.IsNotNull(boardColumnField);
            Assert.AreEqual("Second Value", boardColumnField.Value);
        }

        [Test]
        public void ProcessFields_WithNonFinalRevision_DoesNotAddBoardColumnFieldFromRevisionFields()
        {
            // Arrange
            var executionItem = new ExecutionPlan.ExecutionItem
            {
                Revision = new WiRevision
                {
                    ParentOriginId = "1",

                    Fields = new List<WiField>
                    {
                        new WiField { ReferenceName = WiFieldReference.Title, Value = "Test Title" },
                        new WiField { ReferenceName = WiFieldReference.BoardColumn, Value = "To Do" }
                    },
                },
                OriginId = "1",
                IsFinal = false
            };

            // Act
            _boardColumnCollector.ProcessFields(executionItem);

            // Assert
            var boardColumnField = executionItem.Revision.Fields.FirstOrDefault(f => f.ReferenceName == WiFieldReference.BoardColumn);
            Assert.IsNull(boardColumnField);
            
        }

        [Test]
        public void ProcessFields_MultipleCallsWithDifferentBoardColumnValues_ReturnsCorrectCurrentValue()
        {
            // Arrange
            var firstValue = "ValueOne";
            var secondValue = "ValueTwo";

            var collector = new BoardColumnCollector();
            var executionItem1 = new ExecutionPlan.ExecutionItem
            {
                Revision = new WiRevision
                {
                    ParentOriginId = "1",
                    Fields = new List<WiField>
                    {
                        new WiField { ReferenceName = WiFieldReference.BoardColumn, Value = firstValue }
                    }
                },
                IsFinal = false,
                OriginId = "1"
            };
            var executionItem2 = new ExecutionPlan.ExecutionItem
            {
                Revision = new WiRevision
                {
                    ParentOriginId = "1",
                    Fields = new List<WiField>
                    {
                        new WiField { ReferenceName = WiFieldReference.BoardColumn, Value = secondValue }
                    }
                },
                IsFinal = false,
                OriginId = "1"
            };

            // Act
            collector.ProcessFields(executionItem1);
            collector.ProcessFields(executionItem2);
            var latestValue = collector.GetCurrentValue("1");

            // Assert
            Assert.AreEqual(secondValue, latestValue);
            
        }

        [Test]
        public void ProcessFields_FinalRevisionDoesNotHaveBoardColumn_RetainsPreviousValueAndSetsInTheFinalRevision()
        {
            // Arrange
            var executionItem1 = new ExecutionPlan.ExecutionItem
            {
                Revision = new WiRevision
                {
                    ParentOriginId = "1",
                    Fields = new List<WiField>
                    {
                        new WiField { ReferenceName = WiFieldReference.BoardColumn, Value = "To Do" },
                        new WiField { ReferenceName = WiFieldReference.Title, Value = "First Item" }
                    }
                },
                OriginId = "1",
                IsFinal = false
            };
            var executionItemWithoutBoardColumn = new ExecutionPlan.ExecutionItem
            {
                Revision = new WiRevision
                {
                    ParentOriginId = "1",
                    Fields = new List<WiField>
                    {
                        new WiField { ReferenceName = WiFieldReference.Title, Value = "Second Item" }
                    }
                },
                IsFinal = true,
                OriginId = "1"
            };

            // Act
            _boardColumnCollector.ProcessFields(executionItem1);
            _boardColumnCollector.ProcessFields(executionItemWithoutBoardColumn);

            // Assert
            var boardColumnField = executionItem1.Revision.Fields.FirstOrDefault(f => f.ReferenceName == WiFieldReference.BoardColumn);
            Assert.IsNull(boardColumnField);
            boardColumnField = executionItemWithoutBoardColumn.Revision.Fields.FirstOrDefault(f => f.ReferenceName == WiFieldReference.BoardColumn);
            Assert.IsNotNull(boardColumnField);
        }

        [Test]
        public void GetCurrentValue_WithExistingWorkItemId_ReturnsLatestBoardColumnValue()
        {
            // Arrange
            _boardColumnCollector.ProcessFields(new ExecutionPlan.ExecutionItem
            {
                Revision = new WiRevision
                {
                    ParentOriginId = "1",
                    Fields = new List<WiField>
                    {
                        new WiField { ReferenceName = WiFieldReference.BoardColumn, Value = "To Do" }
                    }
                },
                OriginId = "1",
                IsFinal = true
            });

            // Act
            var currentValue = _boardColumnCollector.GetCurrentValue("1");

            // Assert
            Assert.AreEqual("To Do", currentValue);
        }

        [Test]
        public void GetCurrentValue_WithNonExistingWorkItemId_ReturnsNull()
        {
            // Arrange
            _boardColumnCollector.ProcessFields(new ExecutionPlan.ExecutionItem
            {
                Revision = new WiRevision
                {
                    ParentOriginId = "1",
                    Fields = new List<WiField>
                    {
                        new WiField { ReferenceName = WiFieldReference.BoardColumn, Value = "To Do" }
                    }
                },
                OriginId = "1",
                IsFinal = true
            });

            // Act
            var currentValue = _boardColumnCollector.GetCurrentValue("2");

            // Assert
            Assert.IsNull(currentValue);
        }

        [Test]
        public void GetCurrentValue_WithWorkItemThatHasNoBoardColumnValue_ReturnsNull()
        {
            // Arrange
            _boardColumnCollector.ProcessFields(new ExecutionPlan.ExecutionItem
            {
                Revision = new WiRevision
                {
                    ParentOriginId = "1",
                    Fields = new List<WiField>
                    {
                        new WiField { ReferenceName = WiFieldReference.BoardColumn, Value = "To Do" }
                    }
                },
                OriginId = "1",
                IsFinal = true
            });            
            _boardColumnCollector.ProcessFields(new ExecutionPlan.ExecutionItem
            {
                Revision = new WiRevision
                {
                    ParentOriginId = "2",
                    Fields = new List<WiField>
                    {
                        new WiField { ReferenceName = WiFieldReference.Title, Value = "To Do" }
                    }
                },
                OriginId = "2",
                IsFinal = true
            });

            // Act
            var currentValue = _boardColumnCollector.GetCurrentValue("2");

            // Assert
            Assert.IsNull(currentValue);
        }
    }
}


