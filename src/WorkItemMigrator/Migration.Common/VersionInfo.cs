using System.Diagnostics;

namespace Migration.Common
{
    public static class VersionInfo
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