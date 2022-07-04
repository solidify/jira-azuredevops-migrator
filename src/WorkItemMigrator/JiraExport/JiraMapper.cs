using System;
using System.Collections.Generic;
using System.Linq;

using Common.Config;

using Migration.Common;
using Migration.Common.Config;
using Migration.Common.Log;
using Migration.WIContract;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Migration.Jira-Export.Tests")]

namespace JiraExport
{
    internal class JiraMapper : BaseMapper<JiraRevision>
    {
        private readonly IJiraProvider _jiraProvider;
        private readonly Dictionary<string, FieldMapping<JiraRevision>> _fieldMappingsPerType;
        private readonly HashSet<string> _targetTypes;
        private readonly ConfigJson _config;

        public JiraMapper(IJiraProvider jiraProvider, ConfigJson config) : base(jiraProvider?.GetSettings()?.UserMappingFile)
        {
            _jiraProvider = jiraProvider;
            _config = config;
            _targetTypes = InitializeTypeMappings();
            _fieldMappingsPerType = InitializeFieldMappings();

        }

        #region Mapping definitions

        internal WiItem Map(JiraItem issue)
        {
            var wiItem = new WiItem();

            if (_config.TypeMap.Types != null)
            {
                var type = (from t in _config.TypeMap.Types where t.Source == issue.Type select t.Target).FirstOrDefault();

                if (type != null)
                {
                    var revisions = issue.Revisions.Select(r => MapRevision(r)).ToList();
                    wiItem.OriginId = issue.Key;
                    wiItem.Type = type;
                    wiItem.Revisions = revisions;
                }
                else
                {
                    Logger.Log(LogLevel.Error, $"Type mapping missing for '{issue.Key}' with Jira type '{issue.Type}'. Item was not exported which may cause missing links in issues referencing this item.");
                    return null;
                }
            }
            return wiItem;
        }

        internal Dictionary<string, FieldMapping<JiraRevision>> InitializeFieldMappings()
        {
            Logger.Log(LogLevel.Info, "Initializing Jira field mapping...");

            var commonFields = new FieldMapping<JiraRevision>();
            var typeFields = new Dictionary<string, FieldMapping<JiraRevision>>();

            foreach (var targetType in _targetTypes)
            {
                if (!typeFields.ContainsKey(targetType))
                    typeFields.Add(targetType, new FieldMapping<JiraRevision>());
            }

            foreach (var item in _config.FieldMap.Fields)
            {
                if (item.Source != null)
                {
                    var isCustomField = item.SourceType == "name";
                    Func<JiraRevision, (bool, object)> value;

                    if (item.Mapping?.Values != null)
                    {
                        value = r => FieldMapperUtils.MapValue(r, item.Source, _config);
                    }
                    else if (!string.IsNullOrWhiteSpace(item.Mapper))
                    {
                        switch (item.Mapper)
                        {
                            case "MapTitle":
                                value = r => FieldMapperUtils.MapTitle(r);
                                break;
                            case "MapTitleWithoutKey":
                                value = r => FieldMapperUtils.MapTitleWithoutKey(r);
                                break;
                            case "MapUser":
                                value = IfChanged<string>(item.Source, isCustomField, MapUser);
                                break;
                            case "MapSprint":
                                value = IfChanged<string>(item.Source, isCustomField, FieldMapperUtils.MapSprint);
                                break;
                            case "MapTags":
                                value = IfChanged<string>(item.Source, isCustomField, FieldMapperUtils.MapTags);
                                break;
                            case "MapArray":
                                value = IfChanged<string>(item.Source, isCustomField, FieldMapperUtils.MapArray);
                                break;
                            case "MapRemainingWork":
                                value = IfChanged<string>(item.Source, isCustomField, FieldMapperUtils.MapRemainingWork);
                                break;
                            case "MapRendered":
                                value = r => FieldMapperUtils.MapRenderedValue(r, item.Source, isCustomField, _jiraProvider.GetCustomId(item.Source), _config);
                                break;
                            default:
                                value = IfChanged<string>(item.Source, isCustomField);
                                break;
                        }
                    }
                    else
                    {
                        var dataType = item.Type.ToLower();
                        if (dataType == "double")
                        {
                            value = IfChanged<double>(item.Source, isCustomField);
                        }
                        else if (dataType == "int" || dataType == "integer")
                        {
                            value = IfChanged<int>(item.Source, isCustomField);
                        }
                        else if (dataType == "datetime" || dataType == "date")
                        {
                            value = IfChanged<DateTime>(item.Source, isCustomField);
                        }
                        else
                        {
                            value = IfChanged<string>(item.Source, isCustomField);
                        }
                    }

                    // Check if not-for has been set, if so get all work item types except that one, else for has been set and get those                 
                    var currentWorkItemTypes = !string.IsNullOrWhiteSpace(item.NotFor) ? GetWorkItemTypes(item.NotFor.Split(',')) : item.For.Split(',').ToList();

                    foreach (var wit in currentWorkItemTypes)
                    {
                        try
                        {
                            if (wit == "All" || wit == "Common")
                            {
                                commonFields.Add(item.Target, value);
                            }
                            else
                            {
                                // If we haven't mapped the Type then we probably want to ignore the field
                                if (typeFields.TryGetValue(wit, out FieldMapping<JiraRevision> fm))
                                {
                                    fm.Add(item.Target, value);
                                }
                                else
                                {
                                    Logger.Log(LogLevel.Warning, $"No target type '{wit}' is set, field {item.Source} cannot be mapped.");
                                }
                            }
                        }

                        catch (Exception)
                        {
                            Logger.Log(LogLevel.Warning, $"Ignoring target mapping with key: '{item.Target}', because it is already configured.");
                        }
                    }
                }
            }

            // Now go through the list of built up type fields (which we will eventually get to
            // and then add them to the complete dictionary per type.
            var mappingPerWiType = new Dictionary<string, FieldMapping<JiraRevision>>();
            foreach (KeyValuePair<string, FieldMapping<JiraRevision>> item in typeFields)
            {
                mappingPerWiType.Add(item.Key, MergeMapping(commonFields, item.Value));
            }

            return mappingPerWiType;
        }

        internal List<WiLink> MapLinks(JiraRevision r)
        {
            var links = new List<WiLink>();
            if (r.LinkActions == null)
                return links;

            // map issue links
            foreach (var jiraLinkAction in r.LinkActions)
            {
                var changeType = jiraLinkAction.ChangeType == RevisionChangeType.Added ? ReferenceChangeType.Added : ReferenceChangeType.Removed;

                var link = new WiLink();

                if (_config.LinkMap.Links != null)
                {
                    var linkType = (from t in _config.LinkMap.Links where t.Source == jiraLinkAction.Value.LinkType select t.Target).FirstOrDefault();

                    if (linkType != null)
                    {
                        link.Change = changeType;
                        link.SourceOriginId = jiraLinkAction.Value.SourceItem;
                        link.TargetOriginId = jiraLinkAction.Value.TargetItem;
                        link.WiType = linkType;

                        links.Add(link);
                    }
                }
            }

            // map epic link
            LinkMapperUtils.AddRemoveSingleLink(r, links, _jiraProvider.GetSettings().EpicLinkField, "Epic", _config);

            // map parent
            LinkMapperUtils.AddRemoveSingleLink(r, links, "parent", "Parent", _config);

            // map epic child
            LinkMapperUtils.MapEpicChildLink(r, links, "epic child", "Child", _config);


            return links;
        }

        internal List<WiAttachment> MapAttachments(JiraRevision rev)
        {
            var attachments = new List<WiAttachment>();
            if (rev.AttachmentActions == null)
                return attachments;

            _jiraProvider.DownloadAttachments(rev).Wait();

            foreach (var att in rev.AttachmentActions)
            {
                var change = att.ChangeType == RevisionChangeType.Added ? ReferenceChangeType.Added : ReferenceChangeType.Removed;

                var wiAtt = new WiAttachment()
                {
                    Change = change,
                    AttOriginId = att.Value.Id,
                    FilePath = att.Value.LocalPath,
                    Comment = "Imported from Jira"
                };
                attachments.Add(wiAtt);
            }

            return attachments;
        }

        internal List<WiField> MapFields(JiraRevision r)
        {
            var fields = new List<WiField>();

            if (_config.TypeMap.Types != null)
            {
                var type = (from t in _config.TypeMap.Types where t.Source == r.Type select t.Target).FirstOrDefault();

                if (type != null && _fieldMappingsPerType.TryGetValue(type, out var mapping))
                {
                    foreach (var field in mapping)
                    {
                        try
                        {
                            var fieldreference = field.Key;
                            var (include, value) = field.Value(r);

                            if (include)
                            {
                                Logger.Log(LogLevel.Debug, $"Mapped value '{value}' to field '{fieldreference}'.");
                                fields.Add(new WiField()
                                {
                                    ReferenceName = fieldreference,
                                    Value = value
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Log(ex, $"Error mapping field '{field.Key}' on item '{r.OriginId}'.");
                        }
                    }
                }
            }

            return fields;
        }

        internal WiRevision MapRevision(JiraRevision r)
        {
            Logger.Log(LogLevel.Debug, $"Mapping revision {r.Index}.");

            List<WiAttachment> attachments = MapAttachments(r);
            List<WiField> fields = MapFields(r);
            List<WiLink> links = MapLinks(r);

            return new WiRevision()
            {
                ParentOriginId = r.ParentItem.Key,
                Index = r.Index,
                Time = r.Time,
                Author = MapUser(r.Author),
                Attachments = attachments,
                Fields = fields,
                Links = links,
                AttachmentReferences = attachments.Any()
            };
        }

        protected override string MapUser(string sourceUser)
        {
            if (string.IsNullOrWhiteSpace(sourceUser))
                return null;

            var email = _jiraProvider.GetUserEmail(sourceUser);
            return base.MapUser(email);
        }

        private HashSet<string> InitializeTypeMappings()
        {
            HashSet<string> types = new HashSet<string>();
            _config.TypeMap.Types.ForEach(t => types.Add(t.Target));
            return types;
        }

        private Func<JiraRevision, (bool, object)> IfChanged<T>(string sourceField, bool isCustomField, Func<T, object> mapperFunc = null)
        {
            if (isCustomField)
            {
                var customFieldName = _jiraProvider.GetCustomId(sourceField);
                sourceField = customFieldName;
            }

            return (r) =>
            {
                if (r.Fields.TryGetValue(sourceField.ToLower(), out object value))
                {
                    if (mapperFunc != null)
                    {
                        return (true, mapperFunc((T)value));
                    }
                    else
                    {
                        return (true, (T)value);
                    }
                }
                else
                {
                    return (false, null);
                }
            };
        }

        #endregion
        
        private List<string> GetWorkItemTypes(params string[] notFor)
        {
            List<string> list;
            if (notFor != null && notFor.Any())
            {
                list = _targetTypes.Where(t => !notFor.Contains(t)).ToList();
            }
            else
            {
                list = _targetTypes.ToList();
            }
            return list;
        }
    }
}