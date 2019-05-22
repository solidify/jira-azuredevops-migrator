using Migration.Common;
using System;
using Migration.Common.Log;

namespace WorkItemImport
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            VersionInfo.PrintInfoMessage("Work Item Importer");

            try
            {
                var cmd = new ImportCommandLine(args);
                cmd.Run();
            }
            catch (Exception ex)
            {
                Logger.Log(ex, "Application stopped due to an unexpected exception", LogLevel.Critical);
            }
        }
    }
}
