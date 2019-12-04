using Migration.Common;
using Newtonsoft.Json.Linq;

namespace JiraExport
{
    public class JiraChangeItem
    {
        public JiraChangeItem(JObject item)
        {
            Field = item.ExValue<string>("$.field");
            FieldType = item.ExValue<string>("$.fieldtype");
            FieldId = item.ExValue<string>("$.fieldId");

            From = item.ExValue<string>("$.from");
            FromString = item.ExValue<string>("$.fromString");

            To = item.ExValue<string>("$.to");
            ToString = item.ExValue<string>("$.toString");
        }

        public string Field { get; private set; }
        public string FieldType { get; private set; }
        public string FieldId { get; private set; }
        public string From { get; private set; }
        public string FromString { get; private set; }
        public string To { get; private set; }
        public new string ToString { get; private set; }
    }
}
