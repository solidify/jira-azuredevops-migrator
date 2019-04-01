using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Configuration;

namespace Migration.Common.Log
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }

    public static class Logger
    {
        private const string SEPARATOR = "====================================================================";
        private static string _logFilePath;
        private static LogLevel _logLevel;
        private static List<string> _errors = new List<string>();
        private static List<string> _warnings = new List<string>();
        private static TelemetryClient _telemetryClient = null;
        private static string _sessionId = Guid.NewGuid().ToString();

        static Logger()
        {
            InitApplicationInsights();
        }

        public static void Init(string app, string configFile, string dirPath, string level, bool force)
        {
            Init(app, configFile, dirPath, GetLogLevelFromString(level), force);
        }

        internal static void Init(string app, string configFile, string dirPath, LogLevel level, bool force)
        {
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
            _logFilePath = Path.Combine(dirPath, $"{app}-log-{DateTime.Now.ToString("yyMMdd-HHmmss")}.txt");
            _logLevel = level;

            InitSession(app, configFile, force);
        }

        internal static void InitSession(string app, Dictionary<string, string> arguments)
        {
            var toolVersion = VersionInfo.GetVersionInfo();
            var osVersion = System.Runtime.InteropServices.RuntimeInformation.OSDescription.Trim();
            var machine = System.Environment.MachineName;
            var user = $"{System.Environment.UserDomainName}\\{System.Environment.UserName}";

            ToFile(SEPARATOR);
            ToFile($"{app} Log");
            ToFile(SEPARATOR);
            foreach (var a in arguments)
            {
                ToFile($"{a.Key} {a.Value}");
            }
        }

        public static void StartSession(string message, Dictionary<string, string> context, Dictionary<string, string> properties)
        {
            foreach (var c in context)
            {
                ToFile($"{c.Key} {c.Value}");
            }
            ToFile(SEPARATOR);

            LogEvent(message, properties);
        }

        public static void EndSession(string message, Dictionary<string, string> properties)
        {
            LogEvent(message, properties);

            if (_telemetryClient != null)
            {
                _telemetryClient.Flush();
            }
        }

        private static void InitApplicationInsights()
        {
            var key = ConfigurationManager.AppSettings["applicationInsightsKey"];

            LogInternal(LogLevel.Info, $"Session Id {_sessionId}.");
            LogInternal(LogLevel.Info, string.Format("Application insights {0}.", !string.IsNullOrEmpty(key) ? "enabled." : "disabled"));

            if (!string.IsNullOrEmpty(key))
            {
                TelemetryConfiguration.Active.InstrumentationKey = key;
                _telemetryClient = new TelemetryClient();
                _telemetryClient.Context.Component.Version = VersionInfo.GetVersionInfo();
                _telemetryClient.Context.Session.Id = _sessionId;
            }
        }

        internal static void Init(MigrationContext instance)
        {
            Init(instance.App, instance.ConfigFile, instance.MigrationWorkspace, instance.LogLevel);
        }

        public static void Log(LogLevel level, string message)
        {
            LogInternal(level, message);

            LogApplicationInsights(level, message);

            if (level == LogLevel.Critical)
            {
                _errors.Add(message);
                throw new AbortMigrationException(message);
            }
            else if (level == LogLevel.Error)
            {
                _errors.Add(message);
                Console.Write("Do you want to continue (y/n)? ");
                var answer = Console.ReadKey();
                if (answer.Key == ConsoleKey.N)
                    throw new AbortMigrationException(message);

            }
            else if (level == LogLevel.Warning)
                _warnings.Add(message);
        }

        private static void LogInternal(LogLevel level, string message)
        {
            ToFile(level, message);

            if ((int)level >= (int)_logLevel)
                ToConsole(level, message);
        }

        private static void LogApplicationInsights(LogLevel level, string message)
        {
            if (_telemetryClient != null)
            {
                _telemetryClient.TrackTrace(message, MapLogLevelToApplicationInsightsLevel(level));
            }
        }

        private static SeverityLevel MapLogLevelToApplicationInsightsLevel(LogLevel level)
        {
            SeverityLevel severityLevel = SeverityLevel.Information;
            switch (level)
            {
                case LogLevel.Critical:
                    severityLevel = SeverityLevel.Critical;
                    break;
                case LogLevel.Debug:
                    severityLevel = SeverityLevel.Verbose;
                    break;
                case LogLevel.Error:
                    severityLevel = SeverityLevel.Error;
                    break;
                case LogLevel.Info:
                    severityLevel = SeverityLevel.Information;
                    break;
                case LogLevel.Warning:
                    severityLevel = SeverityLevel.Warning;
                    break;
            }
            return severityLevel;
        }

        public static void Log(Exception ex)
        {
            LogExceptionToApplicationInsights(ex);
            Log(LogLevel.Error, $"[{ex.GetType().ToString()}] {ex.Message}: {Environment.NewLine + ex.StackTrace}");
        }

        public static void LogEvent(string message, Dictionary<string, string> properties)
        {
            Log(LogLevel.Info, $"LogEvent: {message}");
            foreach (var p in properties)
            {
                Log(LogLevel.Info, $"   {p.Key}: {p.Value}");
            }

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
                    Console.WriteLine(message);
                }
            }
            finally
            {
                Console.ResetColor();
            }
        }

        private static LogLevel GetLogLevelFromString(string level)
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

        public static void Summary()
        {
            if ((_warnings != null && _warnings.Any()) || _errors != null && _errors.Any())
            {
                Console.WriteLine(new String('=', 40));
                Console.WriteLine("Summary");
                Console.WriteLine(new String('=', 40));
                if (_warnings.Count > 0)
                {
                    Console.WriteLine("Warnings:");
                    Console.WriteLine(new String('=', 40));
                    foreach (var warning in _warnings)
                    {
                        LogInternal(LogLevel.Warning, warning);
                    }
                    Console.WriteLine(new String('=', 40));
                }
                if (_errors.Count > 0)
                {
                    Console.WriteLine("Errors:");
                    Console.WriteLine(new String('=', 40));
                    foreach (var error in _errors)
                    {
                        LogInternal(LogLevel.Error, error);
                    }
                    Console.WriteLine(new String('=', 40));
                }
            }
        }

        public static int Warnings
        {
            get
            {
                return _warnings.Count;
            }
        }

        public static int Errors
        {
            get
            {
                return _errors.Count;
            }
        }
    }
}
