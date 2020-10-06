using System.Text.RegularExpressions;

namespace Migration.Common
{
    public class CommonFunctions
    {
        // @"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.atlassian\.net\/browse\/"
        public static string ValidateUrl(string linkBack, Regex regex)
        {
            if (!regex.IsMatch(linkBack))
            {
                linkBack += '/';
                if (!regex.IsMatch(linkBack))
                    return null;
            }
            return linkBack;
        }
    }
}
