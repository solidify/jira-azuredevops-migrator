using System;
using System.Text.RegularExpressions;

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
    }
}