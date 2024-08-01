using Serilog;
using System.Text;

namespace NTDLS.Katzebase.Engine.Interactions.Management
{
    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to logging.
    /// </summary>
    public class LogManager
    {
        public static void Debug(string message, Exception ex) => Log.Debug($"{message} {GetExceptionText(ex)}");
        public static void Debug(string message) => Log.Debug(message);
        public static void Debug(Exception ex) => Log.Debug(GetExceptionText(ex));

        public static void Verbose(string message, Exception ex) => Log.Verbose($"{message} {GetExceptionText(ex)}");
        public static void Verbose(string message) => Log.Verbose(message);
        public static void Verbose(Exception ex) => Log.Verbose(GetExceptionText(ex));

        public static void Information(string message, Exception ex) => Log.Information($"{message} {GetExceptionText(ex)}");
        public static void Information(string message) => Log.Information(message);
        public static void Information(Exception ex) => Log.Information(GetExceptionText(ex));

        public static void Warning(string message, Exception ex) => Log.Warning($"{message} {GetExceptionText(ex)}");
        public static void Warning(string message) => Log.Warning(message);
        public static void Warning(Exception ex) => Log.Warning(GetExceptionText(ex));

        public static void Error(string message, Exception ex) => Log.Error($"{message} {GetExceptionText(ex)}");
        public static void Error(string message) => Log.Error(message);
        public static void Error(Exception ex) => Log.Error(GetExceptionText(ex));

        public static void Fatal(string message, Exception ex) => Log.Fatal($"{message} {GetExceptionText(ex)}");
        public static void Fatal(string message) => Log.Fatal(message);
        public static void Fatal(Exception ex) => Log.Fatal(GetExceptionText(ex));

        private static string GetExceptionText(Exception exception)
        {
            try
            {
                var message = new StringBuilder();
                return GetExceptionText(exception, 0, ref message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Critical log exception. Failed to get exception text: {ex.Message}.");
                throw;
            }
        }

        private static string GetExceptionText(Exception exception, int level, ref StringBuilder message)
        {
            try
            {
                if (exception.Message != null && exception.Message != string.Empty)
                {
                    message.AppendFormat("{0} {1}", level, exception.Message);
                }

                if (exception.InnerException != null && level < 100)
                {
                    return GetExceptionText(exception.InnerException, level + 1, ref message);
                }

                return message.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Critical log exception. Failed to get exception text: {ex.Message}.");
                throw;
            }
        }
    }
}
