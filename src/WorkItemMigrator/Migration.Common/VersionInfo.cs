using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Common
{
    public class VersionInfo
    {
        public static string GetVersionInfo()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetCallingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fvi.ProductVersion;
            return version;
        }

        public static string GetCopyrightInfo()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetCallingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fvi.LegalCopyright;
        }
    }
}
