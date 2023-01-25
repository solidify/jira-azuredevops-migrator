using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Migration.Common.Log
{
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Critical = 4
    }

    public static class Logger
    {
        private const string SEPARATOR = "====================================================================";
        private static string _logFilePath;
        private static LogLevel _logLevel;
        private static List<string> _errors = new List<string>();
        private static List<string> _warnings = new List<string>();
        private static TelemetryClient _telemetryClient = null;
        private static bool? _continueOnCritical;

        static Logger()
        {
            InitApplicationInsights();
        }

        public static void Init(string app, string dirPath, string level, string continueOnCritical = null)
        {
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
            _logFilePath = Path.Combine(dirPath, $"{app}-log-{DateTime.Now:yyMMdd-HHmmss}.txt");
            _logLevel = GetLogLevelFromString(level);
            _continueOnCritical = ParseContinueOnCritical(continueOnCritical);
        }

        public static void StartSession(string app, string message, Dictionary<string, string> context, Dictionary<string, string> properties)
        {
            var currentContent = string.Empty;
            if (File.Exists(_logFilePath))
            {
                currentContent = File.ReadAllText(_logFilePath);
            }
            File.Delete(_logFilePath);

            ToFile(SEPARATOR);
            ToFile($"{app} Log");
            ToFile(SEPARATOR);
            foreach (var c in context)
            {
                ToFile($"{c.Key} {c.Value}");
            }
            ToFile(SEPARATOR);
            ToFile(currentContent.Trim());

            LogEvent(message, properties);
        }

        public static void EndSession(string message, Dictionary<string, string> properties)
        {
            LogEvent(message, properties);

            if (_telemetryClient != null)
                _telemetryClient.Flush();
        }

        private static void InitApplicationInsights()
        {
            var key = ConfigurationManager.AppSettings["applicationInsightsKey"];

            if (!string.IsNullOrEmpty(key) && Guid.TryParse(key, out Guid temp))
            {
                TelemetryConfiguration.Active.InstrumentationKey = key;
                _telemetryClient = new TelemetryClient();
                _telemetryClient.Context.Component.Version = VersionInfo.GetVersionInfo();
                _telemetryClient.Context.Session.Id = SessionId;
            }
        }

        public static void Log(LogLevel level, string message)
        {
            LogInternal(level, message);

            if (level == LogLevel.Critical)
            {
                if (!_errors.Contains(message))
                    _errors.Add(message);

                ConsoleKey answer;
                if (!_continueOnCritical.HasValue)
                {
                    Console.Write("Do you want to continue (y/n)? ");
                    answer = Console.ReadKey().Key;
                }
                else
                {
                    answer = _continueOnCritical.Value ? ConsoleKey.Y : ConsoleKey.N;
                }

                if (answer == ConsoleKey.N)
                    throw new AbortMigrationException(message);
            }
            else if (level == LogLevel.Error)
            {
                if (!_errors.Contains(message))
                    _errors.Add(message);
            }
            else if (level == LogLevel.Warning && !_warnings.Contains(message))
            {
                _warnings.Add(message);
                LogTrace(message, level);
            }
        }
        public static void Log(Exception ex, string message, LogLevel logLevel = LogLevel.Error)
        {
            LogExceptionToApplicationInsights(ex);
            Log(logLevel, $"{message + Environment.NewLine}[{ex.GetType()}] {ex}: {Environment.NewLine + ex.StackTrace}");
        }

        private static void LogInternal(LogLevel level, string message)
        {
            if ((int)level >= (int)_logLevel)
            {
                if (level == LogLevel.Debug)
                    message = $"   {message}";
                ToFile(level, message);
                ToConsole(level, message);
            }
        }

        private static void LogTrace(string message, LogLevel level)
        {
            if (_telemetryClient != null)
                _telemetryClient.TrackTrace(message, (SeverityLevel)level);
        }

        private static void LogEvent(string message, Dictionary<string, string> properties)
        {
            if (_telemetryClient != null)
                _telemetryClient.TrackEvent(message, properties);
        }

        private static void LogExceptionToApplicationInsights(Exception ex)
        {
            if (_telemetryClient != null)
                _telemetryClient.TrackException(ex);
        }

        private static void ToFile(LogLevel level, string message)
        {
            string levelPrefix = GetPrefixFromLogLevel(level);
            string dateTime = DateTime.Now.ToString("HH:mm:ss");
            string log = $"[{levelPrefix}][{dateTime}] {message}";
            ToFile(log);
        }

        private static void ToFile(string message)
        {
            if (_logFilePath != null)
                File.AppendAllText(_logFilePath, $"{message}{Environment.NewLine}");
        }

        private static void ToConsole(LogLevel level, string message)
        {
            try
            {
                if ((int)level >= (int)_logLevel)
                {
                    Console.ForegroundColor = GetColorFromLogLevel(level);
                    string levelPrefix = GetPrefixFromLogLevel(level);
                    string dateTime = DateTime.Now.ToString("HH:mm:ss");

                    string log = $"[{levelPrefix}][{dateTime}] {message}";
                    Console.WriteLine(log);
                }
            }
            finally
            {
                Console.ResetColor();
            }
        }

        public static LogLevel GetLogLevelFromString(string level)
        {
            LogLevel logLevel = LogLevel.Debug;
            switch (level)
            {
                case "Info": logLevel = LogLevel.Info; break;
                case "Debug": logLevel = LogLevel.Debug; break;
                case "Warning": logLevel = LogLevel.Warning; break;
                case "Error": logLevel = LogLevel.Error; break;
                case "Critical": logLevel = LogLevel.Critical; break;
                default:
                    break;
            }
            return logLevel;
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

        private static bool? ParseContinueOnCritical(string continueOnCritical)
        {
            if (string.IsNullOrEmpty(continueOnCritical))
            {
                return null;
            }

            var success = bool.TryParse(continueOnCritical, out var result);

            if (!success)
            {
                return null;
            }

            return result;
        }

        public static int Warnings => _warnings.Count;

        public static int Errors => _errors.Count;

        public static string SessionId { get; } = Guid.NewGuid().ToString();

        public static string TelemetryStatus => _telemetryClient != null ? "Enabled" : "Disabled";
    }
}
