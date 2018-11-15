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

    public enum TemplateType
    {
        Scrum,
        Agile,
        CMMI
    }

    public static class WorkItemType
    {
        public static string ProductBacklogItem => "Product Backlog Item";
        public static string UserStory => "User Story";
        public static string Requirement => "Requirement";
        public static string Bug => "Bug";
        public static string Task => "Task";
        public static string Epic => "Epic";
        public static string Feature => "Feature";

        public static List<string> GetWorkItemTypes(string notFor = "")
        {
            var list = new List<string>();
            var properties = typeof(WorkItemType).GetProperties();
            foreach (var prop in properties)
            {
                var value = prop.GetValue(typeof(WorkItemType)).ToString();
                if (!string.IsNullOrWhiteSpace(notFor))
                {
                    if (value != notFor)
                    {
                        list.Add(value);
                    }
                }
                else
                {
                    list.Add(value);
                }
            }
            return list;
        }
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
            return $"({ParentOriginId}, {Index})";
        }
    }
}