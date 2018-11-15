using Migration.Common;
using System;

namespace WorkItemImport
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Logger.Log(LogLevel.Info, $"Work Item Importer v{VersionInfo.GetVersionInfo()}");
            Logger.Log(LogLevel.Info, VersionInfo.GetCopyrightInfo());

            try
            {
                var cmd = new ImportCommandLine(args);
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
