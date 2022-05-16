using Common.Config;
using Migration.Common.Log;
using Migration.WIContract;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JiraExport
{
    public static class LinkMapperUtils
    {

        public static void MapEpicChildLink(JiraRevision r, List<WiLink> links, string field, string type, ConfigJson config)
        {
            if (r.Fields.TryGetValue(field, out object value))
            {
                var parentKeyStr = r.OriginId.Substring(r.OriginId.LastIndexOf("-", StringComparison.InvariantCultureIgnoreCase) + 1);
                var childKeyStr = value?.ToString().Substring(r.OriginId.LastIndexOf("-", StringComparison.InvariantCultureIgnoreCase) + 1);

                if (int.TryParse(parentKeyStr, out var parentKey) && int.TryParse(childKeyStr, out var childKey))
                {
                    if (parentKey > childKey)
                        AddSingleLink(r, links, field, type, config);
                }
            }
        }

        /// <summary>
        /// Add or remove single link
        /// </summary>
        /// <param name="r"></param>
        /// <param name="links"></param>
        /// <param name="field"></param>
        /// <param name="type"></param>
        /// <returns>True if link is added, false if it's not</returns>
        public static void AddRemoveSingleLink(JiraRevision r, List<WiLink> links, string field, string type, ConfigJson config)
        {
            if (r.Fields.TryGetValue(field, out object value))
            {
                var changeType = value == null ? ReferenceChangeType.Removed : ReferenceChangeType.Added;
                var linkType = (from t in config.LinkMap.Links where t.Source == type select t.Target).FirstOrDefault();

                // regardless if action is add or remove, as there can be only one, we remove previous epic link if it exists
                if (r.Index != 0)
                {
                    var prevLinkValue = r.ParentItem.Revisions[r.Index - 1].GetFieldValue(field);
                    // if previous value is not null, add removal of previous link
                    if (!string.IsNullOrWhiteSpace(prevLinkValue))
                    {
                        var removeLink = new WiLink()
                        {
                            Change = ReferenceChangeType.Removed,
                            SourceOriginId = r.ParentItem.Key,
                            TargetOriginId = prevLinkValue,
                            WiType = linkType
                        };

                        links.Add(removeLink);
                    }
                }

                if (changeType == ReferenceChangeType.Added)
                {
                    string linkedItemKey = (string)value;

                    var link = new WiLink()
                    {
                        Change = changeType,
                        SourceOriginId = r.ParentItem.Key,
                        TargetOriginId = linkedItemKey,
                        WiType = linkType,
                    };

                    links.Add(link);
                }
            }
        }

        // TODO: LinkMapper, AttachmentMapper, FieldMappers (title, sprint, etc...)
        // TODO: Move revision helper functions to RevisionUtility.cs
        public static void AddSingleLink(JiraRevision r, List<WiLink> links, string field, string type, ConfigJson config)
        {
            if (r.Fields.TryGetValue(field, out object value))
            {
                var changeType = value == null ? ReferenceChangeType.Removed : ReferenceChangeType.Added;
                var linkType = (from t in config.LinkMap.Links where t.Source == type select t.Target).FirstOrDefault();


                if (changeType == ReferenceChangeType.Added)
                {
                    string linkedItemKey = (string)value;

                    if (string.IsNullOrEmpty(linkType))
                    {
                        Logger.Log(LogLevel.Warning, $"Cannot add 'Child' {linkedItemKey} link to 'Parent' {r.ParentItem.Key}, 'Child' link-map configuration missing.");
                        return;
                    }

                    var link = new WiLink()
                    {
                        Change = changeType,
                        SourceOriginId = r.ParentItem.Key,
                        TargetOriginId = linkedItemKey,
                        WiType = linkType,
                    };

                    links.Add(link);
                }
            }
        }

    }
}