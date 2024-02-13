using Migration.Common.Log;
using System;

namespace JiraExport
{
    static class Program
    {
        static int Main(string[] args)
        {
            VersionInfo.PrintInfoMessage("Jira Exporter");

            try
            {
                var cmd = new JiraCommandLine(args);
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
