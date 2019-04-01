using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common.Config;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Migration.Common.Log;

namespace Migration.Common.Config
{
    public class ConfigReaderJson : IConfigReader
    {
        private readonly string FilePath;
        private string JsonText;

        public ConfigReaderJson(string filePath)
        {
            FilePath = filePath;
        }

        public ConfigJson Deserialize()
        {
            LoadFromFile(FilePath);
            return DeserializeText(JsonText);
        }

        public void LoadFromFile(string filePath)
        {
            try
            {
                JsonText = GetJsonFromFile(filePath);
            }
            catch (FileNotFoundException)
            {
                Logger.Log(LogLevel.Error, $"Required JSON configuration file '{filePath}' was not found. Please ensure that this file is in the correct location.");
                throw;
            }
            catch (PathTooLongException)
            {
                Logger.Log(LogLevel.Error, $"Required JSON configuration file '{filePath}' could not be accessed because the file path is too long. Please store your files for this application in a folder location with a shorter path name.");
                throw;
            }
            catch (UnauthorizedAccessException)
            {
                Logger.Log(LogLevel.Error, $"Cannot read from the JSON configuration file '{filePath}' because you are not authorized to access it. Please try running this application as administrator or moving it to a folder location that does not require special access.");
                throw;
            }
            catch (Exception)
            {
                Logger.Log(LogLevel.Error, $"Cannot read from the JSON configuration file '{filePath}'. Please ensure it is formatted properly.");
                throw;
            }
        }

        public string GetJsonFromFile(string filePath)
        {
            return File.ReadAllText(filePath);
        }

        public ConfigJson DeserializeText(string input)
        {
            ConfigJson result = null;
            try
            {
                result = JsonConvert.DeserializeObject<ConfigJson>(input);
                var obj = JObject.Parse(input);

                var fields = obj.SelectToken("field-map.field").Select(jt => jt.ToObject<Field>()).ToList();
                if (result.FieldMap.Fields == null)
                {
                    result.FieldMap.Fields = new List<Field>();
                }
                result.FieldMap.Fields.AddRange(fields);

                var links = obj.SelectToken("link-map.link").Select(li => li.ToObject<Link>()).ToList();
                if(result.LinkMap.Links == null)
                {
                    result.LinkMap.Links = new List<Link>();
                }
                result.LinkMap.Links.AddRange(links);

                var types = obj.SelectToken("type-map.type").Select(li => li.ToObject<Type>()).ToList();
                if (result.TypeMap.Types == null)
                {
                    result.TypeMap.Types = new List<Type>();
                }
                result.TypeMap.Types.AddRange(types);

            }
            catch (Exception)
            {
                Logger.Log(LogLevel.Error, "Cannot deserialize the JSON text from configuration file. Please ensure it is formatted properly.");
                throw;
            }
            return result;
        }
    }
}