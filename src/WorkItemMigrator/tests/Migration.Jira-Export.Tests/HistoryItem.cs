using Newtonsoft.Json.Linq;
using System;

namespace Migration.Jira_Export.Tests
{
    public class HistoryItem
    {
        public long Id { get; set; } = 0;
        public DateTime Created { get; set; } = DateTime.Now;
        public string Field { get; set; } = string.Empty;
        public string FieldType { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public string FromString { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public new string ToString { get; set; } = string.Empty;

        public JObject ToJObject()
        {
            return JObject.Parse($@"
            {{
                'id': {Id},
                'author': 'unittest',
                'created': '{Created:yyyy - MM - ddTHH:mm: ss.fffZ}',
                'items': [
                {{
                  'field': '{Field}',
                  'fieldtype': '{FieldType}',
                  'from': '{From}',
                  'fromString': '{FromString}',
                  'to': '{To}',
                  'toString': '{ToString}'
                }}
              ]
            }}");
        }
    }
}
