using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Migration.WIContract;

namespace Migration.Common
{
    public static class RevisionUtility
    {
        private static TimeSpan _deltaTime = TimeSpan.FromMilliseconds(50);

        public static DateTime NextValidDeltaRev(DateTime current, DateTime? next = null)
        {
            if (next == null || current + _deltaTime < next)
                return current + _deltaTime;

            TimeSpan diff = next.Value - current;
            var middle = new TimeSpan(diff.Ticks / 2);
            return current + middle;
        }

        public static string ReplaceHtmlElements(string html)
        {
            string imageWrapPattern = "<span class=\"image-wrap\".*?>.*?(<img .*? />).*?</span>";
            html = Regex.Replace(html, imageWrapPattern, m => m.Groups[1]?.Value);

            string userLinkPattern = "<a href=.*? class=\"user-hover\" .*?>(.*?)</a>";
            html = Regex.Replace(html, userLinkPattern, m => m.Groups[1]?.Value);

            return html;
        }


        public static T GetFieldValueOrDefault<T>(this List<WiField> fields, string refName)
        {
            if (fields == null)
                return default(T);

            if (fields.Count == 0)
                return default(T);

            var value = fields.FirstOrDefault(x => x.ReferenceName.Equals(refName));

            if (value == null)
                return default(T);

            if (value.Value == null)
                return default(T);

            return (T)value.Value;
        }

        public static bool HasAnyByRefName(this List<WiField> fields, string refName)
        {
            if (fields == null)
                return false;

            if (fields.Count == 0)
                return false;

            if (fields.Any(f => f.ReferenceName.Equals(refName, StringComparison.InvariantCultureIgnoreCase)))
                return true;

            return false;
        }

    }
}