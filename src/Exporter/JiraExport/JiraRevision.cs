using System;
using System.Collections.Generic;
using System.Linq;
using Migration.Common;

namespace JiraExport
{
    public enum RevisionChangeType
    {
        Added,
        Removed
    }

    public class RevisionAction<T>
    {
        public RevisionChangeType ChangeType { get; set; }
        public T Value { get; set; }

        public override string ToString()
        {
            return $"{ChangeType.ToString()} {Value.ToString()}";
        }
    }

    public class JiraRevision : ISourceRevision, IComparable<JiraRevision>
    {
        public DateTime Time { get; set; }

        public string Author { get; set; }

        public Dictionary<string, object> Fields { get; set; }
        public List<RevisionAction<JiraLink>> LinkActions { get; set; }

        public List<RevisionAction<JiraAttachment>> AttachmentActions { get; set; }
        public JiraItem ParentItem { get; private set; }
        public int Index { get; internal set; }

        public string OriginId => ParentItem.Key;

        public string Type => ParentItem.Type;


        public JiraRevision(JiraItem parentItem)
        {
            ParentItem = parentItem;
        }

        public int CompareTo(JiraRevision other)
        {
            int t = this.Time.CompareTo(other.Time);
            if (t != 0)
                return t;

            return this.ParentItem.Key.CompareTo(other.ParentItem.Key);
        }

        public string GetFieldValue(string fieldName)
        {
            return (string)(((IEnumerable<JiraRevision>)ParentItem.Revisions)
                .Reverse()
                .SkipWhile(r => r.Index > this.Index)
                .FirstOrDefault(r => r.Fields.ContainsKey(fieldName))
                ?.Fields[fieldName]);
        }
    }
}