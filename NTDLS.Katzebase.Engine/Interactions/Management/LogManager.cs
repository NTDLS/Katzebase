using NTDLS.Katzebase.Client.Exceptions;
using Serilog;
using System.Diagnostics;
using System.Text;
using static NTDLS.Katzebase.Client.KbConstants;

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
        public static void Error(Exception ex) => Log.Error(GetExceptionText(ex));

        public static void Fatal(string message, Exception ex) => Log.Fatal($"{message} {GetExceptionText(ex)}");
        public static void Fatal(string message) => Log.Fatal(message);
        public static void Fatal(Exception ex) => Log.Fatal(GetExceptionText(ex));

        public static void Exception(string message, Exception givenException)
        {
            if (givenException is KbExceptionBase kbException)
            {
                switch (kbException.Severity)
                {
                    case KbLogSeverity.Verbose:
                        Log.Verbose($"{message} {GetExceptionText(kbException)}");
                        break;
                    case KbLogSeverity.Debug:
                        Log.Debug($"{message} {GetExceptionText(kbException)}");
                        break;
                    case KbLogSeverity.Information:
                        Log.Information($"{message} {GetExceptionText(kbException)}");
                        break;
                    case KbLogSeverity.Warning:
                        Log.Warning($"{message} {GetExceptionText(kbException)}");
                        break;
                    case KbLogSeverity.Error:
                        Log.Error($"{message} {GetExceptionText(kbException)}");
                        break;
                    case KbLogSeverity.Fatal:
                        Log.Fatal($"{message} {GetExceptionText(kbException)}");
                        break;
                }
            }
            else
            {
                Log.Error($"{message} {GetExceptionText(givenException)}");
            }
        }

        public static void Exception(Exception givenException)
        {
            if (givenException is KbExceptionBase kbException)
            {
                switch (kbException.Severity)
                {
                    case KbLogSeverity.Verbose:
                        Log.Verbose(GetExceptionText(kbException));
                        break;
                    case KbLogSeverity.Debug:
                        Log.Debug(GetExceptionText(kbException));
                        break;
                    case KbLogSeverity.Information:
                        Log.Information(GetExceptionText(kbException));
                        break;
                    case KbLogSeverity.Warning:
                        Log.Warning(GetExceptionText(kbException));
                        break;
                    case KbLogSeverity.Error:
                        Log.Error(GetExceptionText(kbException));
                        break;
                    case KbLogSeverity.Fatal:
                        Log.Fatal(GetExceptionText(kbException));
                        break;
                }
            }
            else
            {
                Log.Error(GetExceptionText(givenException));
            }
        }

        private static string GetExceptionText(Exception exception)
        {
            try
            {
                var message = new StringBuilder();
                return GetExceptionText(exception, 0, ref message);
            }
            catch (Exception ex)
            {
                Error($"Critical log exception. Failed to get exception text: {ex.Message}.");
                throw;
            }
        }

        private static string GetExceptionText(Exception exception, int level, ref StringBuilder message)
        {
            try
            {
                if (exception.Message != null && exception.Message != string.Empty)
                {
                    message.Append(exception.Message);
                }

                if (exception.InnerException != null && level < 100)
                {
                    return GetExceptionText(exception.InnerException, level + 1, ref message);
                }

                return message.ToString();
            }
            catch (Exception ex)
            {
                Error($"Critical log exception. Failed to get exception text: {ex.Message}.");
                throw;
            }
        }
    }
}
