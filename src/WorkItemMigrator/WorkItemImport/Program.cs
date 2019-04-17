﻿using Migration.Common;
using System;
using Migration.Common.Log;

namespace WorkItemImport
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine($"Work Item Importer v{VersionInfo.GetVersionInfo()}");
            Console.WriteLine(VersionInfo.GetCopyrightInfo());

            try
            {
                var cmd = new ImportCommandLine(args);
                cmd.Run();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "Closing application due to an unexpected exception: " + ex.Message);
            }

#if DEBUG
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
#endif
        }


    }
}
