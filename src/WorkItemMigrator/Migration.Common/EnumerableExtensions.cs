using System.Collections.Generic;
using System.Linq;

namespace Migration.Common
{
    public static class EnumerableExtensions
    {
        public static bool IsAny<T>(this IEnumerable<T> data)
        {
            return data != null && data.Any();
        }
    }
}
