using Migration.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JiraExport
{
    class Program
    {
        static void Main(string[] args)
        {
            Logger.Log(LogLevel.Info, $"Jira Exporter v{VersionInfo.GetVersionInfo()}");
            Logger.Log(LogLevel.Info, VersionInfo.GetCopyrightInfo());

            try
            {
                var cmd = new JiraCommandLine(args);
                cmd.Run();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "Closing application due to an unexpected exception: " + ex.Message);
            }
            finally
            {
                Logger.Summary();
            }

#if DEBUG
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
#endif

        }
    }
}
