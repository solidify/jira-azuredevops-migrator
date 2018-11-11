using System;
using System.Collections.Generic;
using System.IO;

namespace Migration.Common
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }

    public class Logger
    {
        private static string _logFilePath;
        private static LogLevel _logLevel;
        private static List<string> Errors = new List<string>();
        private static List<string> Warnings = new List<string>();

        public static void Init(string dirPath, LogLevel level)
        {
            if(!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
            _logFilePath = Path.Combine(dirPath, $"log.{Guid.NewGuid().ToString()}.txt");
            _logLevel = level;
        }

        internal static void Init(MigrationContext instance)
        {
            Init(instance.MigrationWorkspace, instance.LogLevel);
        }

        public static void Log(LogLevel level, string message)
        {
            LogInternal(level, message);

            if (level == LogLevel.Critical)
            {
                Errors.Add(message);
                throw new AbortMigrationException(message);
            }
            else if (level == LogLevel.Error)
            {
                Errors.Add(message);
                Console.Write("Do you want to continue (y/n)? ");
                var answer = Console.ReadKey();
                if (answer.Key == ConsoleKey.N)
                    throw new AbortMigrationException(message);

            }
            else if (level == LogLevel.Warning)
                Warnings.Add(message);
        }

        private static void LogInternal(LogLevel level, string message)
        {
            ToFile(level, message);

            if ((int)level >= (int)_logLevel)
                ToConsole(level, message);
        }

        public static void Log(Exception ex)
        {
            Log(LogLevel.Error, $"[{ex.GetType().ToString()}] {ex.Message}: {Environment.NewLine + ex.StackTrace}");
        }

        private static void ToFile(LogLevel level, string message)
        {
            string levelPrefix = GetPrefixFromLogLevel(level);
            string dateTime = DateTime.Now.ToString("HH:mm:ss");

            string log = $"[l:{levelPrefix}][d:{dateTime}] {message}{Environment.NewLine}";
            if (_logFilePath != null)
                File.AppendAllText(_logFilePath, log);
        }

        private static void ToConsole(LogLevel level, string message)
        {
            try
            {
                if ((int)level >= (int)_logLevel)
                {
                    Console.ForegroundColor = GetColorFromLogLevel(level);
                    Console.WriteLine(message);
                }
            }
            finally
            {
                Console.ResetColor();
            }
        }

        private static ConsoleColor GetColorFromLogLevel(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Debug: return ConsoleColor.Gray;
                case LogLevel.Info: return ConsoleColor.White;
                case LogLevel.Warning: return ConsoleColor.Yellow;
                case LogLevel.Error:
                case LogLevel.Critical: return ConsoleColor.Red;
                default: return ConsoleColor.Gray;
            }

        }

        private static string GetPrefixFromLogLevel(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Debug: return "D";
                case LogLevel.Info: return "I";
                case LogLevel.Warning: return "W";
                case LogLevel.Error: return "E";
                case LogLevel.Critical: return "C";
                default: return "I";
            }
        }

        public static void Summary()
        {
            foreach (var warning in Warnings)
                LogInternal(LogLevel.Warning, warning);

            foreach (var error in Errors)
                LogInternal(LogLevel.Error, error);
        }
    }
}
