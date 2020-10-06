using System.Text.RegularExpressions;

namespace Migration.Common
{
    public class CommonFunctions
    {
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
