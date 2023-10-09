using System;
using Migration.Common.Log;

namespace WorkItemImport
{
    static class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            VersionInfo.PrintInfoMessage("Work Item Importer");

            try
            {
                var cmd = new ImportCommandLine(args);
                return cmd.Run();
            }
            catch (Exception ex)
            {
                Logger.Log(ex, "Application stopped due to an unexpected exception", LogLevel.Critical);
                return -1;
            }
        }
    }
}
