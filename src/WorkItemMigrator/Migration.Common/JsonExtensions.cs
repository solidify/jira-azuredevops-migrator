using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Migration.Common
{
    public static class JsonExtensions
    {
        public static T ExValue<T>(this JToken token, string path)
        {
            if (!token.HasValues)
                return default(T);

            var value = token.SelectToken(path, false);

            if (value == null)
                return default(T);

            return value.Value<T>();
        }

        public static IEnumerable<T> GetValues<T>(this JToken token, string path)
        {
            var value = token.SelectToken(path, false);
            return value.Values<T>();
        }
    }
}