using NTDLS.Katzebase.Api.Exceptions;
using Serilog;
using System.Diagnostics;
using static NTDLS.Katzebase.Api.KbConstants;

namespace NTDLS.Katzebase.Engine.Interactions.Management
{
    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to logging.
    /// </summary>
    public static class LogManager
    {
        private static string GetCallerFunctionName()
        {
            var frame = (new StackTrace()).GetFrame(2); // 0 would be this method itself, 1 is the caller
            if (frame == null)
            {
                return string.Empty;
            }

            var methodBase = frame.GetMethod();
            if (methodBase == null)
            {
                return string.Empty;
            }

            var classType = methodBase.DeclaringType;
            if (classType == null || classType.Namespace == null)
            {
                return string.Empty;
            }

            string namespaceName = classType.Namespace;
            string className = classType.Name;
            string methodName = methodBase.Name;

            return $"{namespaceName}.{className}.{methodName}";
        }

        public static void Trace(string message) => Log.Verbose($"{GetCallerFunctionName()}: {message}");
        public static void Verbose(string message) => Log.Verbose(message);
        public static void Debug(string message) => Log.Debug(message);
        public static void Information(string message) => Log.Information(message);
        public static void Warning(string message) => Log.Warning(message);

        public static void Error(string message, Exception ex) => Exception(message, ex);
        public static void Error(string message) => Log.Error(message);
        public static void Error(Exception ex) => Log.Error(ex.GetBaseException().Message);

        public static void Fatal(string message, Exception ex) => Log.Fatal($"{message} {ex.GetBaseException().Message}");
        public static void Fatal(string message) => Log.Fatal(message);
        public static void Fatal(Exception ex) => Log.Fatal(ex.GetBaseException().Message);

        public static void Exception(string message, Exception givenException)
        {
            if (givenException is KbExceptionBase kbException)
            {
                switch (kbException.Severity)
                {
                    case KbLogSeverity.Verbose:
                        Log.Verbose($"{message} {kbException.GetBaseException().Message}");
                        break;
                    case KbLogSeverity.Debug:
                        Log.Debug($"{message} {kbException.GetBaseException().Message}");
                        break;
                    case KbLogSeverity.Information:
                        Log.Information($"{message} {kbException.GetBaseException().Message}");
                        break;
                    case KbLogSeverity.Warning:
                        Log.Warning($"{message} {kbException.GetBaseException().Message}");
                        break;
                    case KbLogSeverity.Error:
                        Log.Error($"{message} {kbException.GetBaseException().Message}");
                        break;
                    case KbLogSeverity.Fatal:
                        Log.Fatal($"{message} {kbException.GetBaseException().Message}");
                        break;
                }
            }
            else
            {
                Log.Error($"{message} {givenException.GetBaseException().Message}");
            }
        }

        public static void Exception(Exception givenException)
        {
            if (givenException is KbExceptionBase kbException)
            {
                switch (kbException.Severity)
                {
                    case KbLogSeverity.Verbose:
                        Log.Verbose(kbException.GetBaseException().Message);
                        break;
                    case KbLogSeverity.Debug:
                        Log.Debug(kbException.GetBaseException().Message);
                        break;
                    case KbLogSeverity.Information:
                        Log.Information(kbException.GetBaseException().Message);
                        break;
                    case KbLogSeverity.Warning:
                        Log.Warning(kbException.GetBaseException().Message);
                        break;
                    case KbLogSeverity.Error:
                        Log.Error(kbException.GetBaseException().Message);
                        break;
                    case KbLogSeverity.Fatal:
                        Log.Fatal(kbException.GetBaseException().Message);
                        break;
                }
            }
            else
            {
                Log.Error(givenException.GetBaseException().Message);
            }
        }
    }
}
