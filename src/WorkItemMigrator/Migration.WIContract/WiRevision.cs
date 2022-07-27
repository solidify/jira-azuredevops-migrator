using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Migration.WIContract
{
    public enum ReferenceChangeType
    {
        Added,
        Removed
    }

    public class WiRevision
    {
        public WiRevision()
        {
            Fields = new List<WiField>();
            Links = new List<WiLink>();
            Attachments = new List<WiAttachment>();
        }

        [JsonIgnore]
        public string ParentOriginId { get; set; }
        public string Author { get; set; }
        public DateTime Time { get; set; } = DateTime.Now;
        public int Index { get; set; } = 1;
        public List<WiField> Fields { get; set; }
        public List<WiLink> Links { get; set; }
        public List<WiAttachment> Attachments { get; set; }

        [DefaultValue(false)]
        public bool AttachmentReferences { get; set; } = false;

        public override string ToString()
        {
            return $"'{ParentOriginId}', rev {Index}";
        }
    }
}