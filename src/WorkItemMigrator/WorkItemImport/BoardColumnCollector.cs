using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Migration.WIContract;

namespace WorkItemImport
{
    /// <summary>
    /// Implementations of this interface will track the value of a field over multiple revisions for each item.  They can
    /// then be queried for the 'current' value of the field as per the latest revision they have seen the a field in.
    /// </summary>
    /// <typeparam name="T">The type used when storing and retrieving the field's value.</typeparam>
    public interface IFieldCollector<T>
    {
        /// <summary>
        /// Collects the field values in a similar way to <see cref="CollectValues"/> but also tests <see cref="ExecutionPlan.ExecutionItem.IsFinal"/>.  For non-final
        /// revisions, the collected fields are also removed from the Fields array.  For final revisions, adds or updates
        /// the Field to contain the latest value of each collected field (whether collected from this revision or a previous one). 
        /// Usually implemented as a combination of <see cref="GetCurrentValue"/> and <see cref="CollectValues"/>
        /// </summary>
        /// <param name="executionItem">The execution item containing the revision to process.</param>
        /// <remarks> It is acceptable for a revision to not contain a value for the field(s) of interest. </remarks>
        void ProcessFields(ExecutionPlan.ExecutionItem executionItem);
        
        /// <summary>
        /// Return the 'current' value collected for the field(s) as per the latest revision of the given work item (that specified a value for this field).
        /// </summary>
        /// <param name="workItemId"></param>
        /// <returns></returns>
        T GetCurrentValue(string workItemId);

        /// <summary>
        /// Collect field value(s) from the given revision, updating the internal collection with the latest value(s).
        /// The revision provided in each call is considered to overwrite value(s) collected during previous calls.
        /// This method does not modify the revision - unlike <see cref="ProcessFields"/>.
        /// </summary>
        /// <param name="revision">The work item revision to collect field value(s) from.</param>
        /// <remarks> It is acceptable for a revision to not contain a value for the field(s) of interest. </remarks>
        void CollectValues(WiRevision revision);
    }
    
    /// <summary>
    /// Collects System.BoardColumn values from each revision in order to provide the final value for the last revision.
    /// Expected usage is to call ProcessFields on each revision, which will cause BoardColumn to be removed from all revisions except
    /// the last, and the final value to be updated/inserted into the BoardColumn field of the final revision.
    /// </summary>
    public class BoardColumnCollector : IFieldCollector<string>
    {
        private readonly Dictionary<string, string> _collection = new Dictionary<string, string>();

        /// <inheritdoc/>
        public void CollectValues(WiRevision revision)
        {
            var boardColumn = revision?.Fields?.FirstOrDefault(f => f.ReferenceName == WiFieldReference.BoardColumn)?.Value as string;
            if (!string.IsNullOrEmpty(boardColumn))
            {
                _collection[revision.ParentOriginId] = boardColumn;
            }
        }

        /// <inheritdoc/>
        public string GetCurrentValue(string workItemId)
        {
            return _collection.TryGetValue(workItemId, out var value) ? value : null;
        }

        /// <summary>
        /// Collects the BoardColumn value and removes the field.  Except in the final revision the field is explicitly inserted with the latest value.
        /// </summary>
        /// <param name="executionItem">The execution item containing the revision to process.</param>
        /// <inheritdoc/>
        public void ProcessFields(ExecutionPlan.ExecutionItem executionItem)
        {
            CollectValues(executionItem.Revision);
            if (executionItem.IsFinal)
            {
                var boardColumnValue = GetCurrentValue(executionItem.OriginId);
                if (!string.IsNullOrWhiteSpace(boardColumnValue))
                {
                    executionItem.Revision.Fields.RemoveAll(i => i.ReferenceName == WiFieldReference.BoardColumn);
                    executionItem.Revision.Fields.Add(new WiField { ReferenceName = WiFieldReference.BoardColumn, Value = boardColumnValue });
                }
            }
            else
            {
                executionItem.Revision.Fields.RemoveAll(f => f.ReferenceName == WiFieldReference.BoardColumn);
            }
        }
    }
}
