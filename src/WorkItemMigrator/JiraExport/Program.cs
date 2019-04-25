using Migration.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Migration.Common.Log;

namespace JiraExport
{
    static class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"Jira Exporter v{VersionInfo.GetVersionInfo()}");
            Console.WriteLine(VersionInfo.GetCopyrightInfo());

            try
            {
                var cmd = new JiraCommandLine(args);
                cmd.Run();
            }
            catch (Exception ex)
            {
                Logger.Log(ex, "Application stopped due to an unexpected exception", LogLevel.Critical);
            }
        }
    }
}
