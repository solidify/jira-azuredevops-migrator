using Migration.Common.Log;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Migration.WIContract
{
    public class WiItemProvider
    {
        private readonly string _itemsDir;

        public WiItemProvider(string itemsDir)
        {
            _itemsDir = itemsDir;
        }

        public WiItem Load(string originId)
        {
            var path = Path.Combine(_itemsDir, $"{originId}.json");
            return LoadFile(path);
        }

        private WiItem LoadFile(string path)
        {
            var serialized = File.ReadAllText(path);
            var deserialized = JsonConvert.DeserializeObject<WiItem>(serialized, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore});

            foreach (var rev in deserialized.Revisions)
                rev.ParentOriginId = deserialized.OriginId;

            return deserialized;
        }

        public void Save(WiItem item)
        {
            string path = Path.Combine(_itemsDir, $"{item.OriginId}.json");
            var serialized = JsonConvert.SerializeObject(item);
            File.WriteAllText(path, serialized);
        }

        public IEnumerable<WiItem> EnumerateAllItems()
        {
            var result = new List<WiItem>();

            foreach (var filePath in Directory.EnumerateFiles(_itemsDir, "*.json"))
            {
                try
                {
                    result.Add(LoadFile(filePath));
                }
                catch(Exception)
                {
                    Logger.Log(LogLevel.Error, $"Failed to load '{Path.GetFileName(filePath)}' (perhaps not a migration file?).");
                }
            }
            return result;
        }
    }
}