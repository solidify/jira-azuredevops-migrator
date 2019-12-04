using Common.Config;

namespace Migration.Common.Config
{
    public interface IConfigReader
    {
        ConfigJson Deserialize();

        void LoadFromFile(string filePath);

        string GetJsonFromFile(string filePath);

        ConfigJson DeserializeText(string input);
    }
}