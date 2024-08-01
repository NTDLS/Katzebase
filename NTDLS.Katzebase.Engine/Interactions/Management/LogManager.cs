using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Logging;
using Serilog;
using System.Text;
using static NTDLS.Katzebase.Client.KbConstants;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Interactions.Management
{
    /// <summary>
    /// Public core class methods for locking, reading, writing and managing tasks related to logging.
    /// </summary>
    public class LogManager
    {
        private readonly EngineCore _core;

        public LogManager(EngineCore core)
        {
            _core = core;
        }

        public void Debug(string message, Exception ex) => Log.Debug($"{message} {GetExceptionText(ex)}");
        public void Debug(string message) => Log.Debug(message);
        public void Debug(Exception ex) => Log.Debug(GetExceptionText(ex));

        public void Verbose(string message, Exception ex) => Log.Verbose($"{message} {GetExceptionText(ex)}");
        public void Verbose(string message) => Log.Verbose(message);
        public void Verbose(Exception ex) => Log.Verbose(GetExceptionText(ex));

        public void Warning(string message, Exception ex) => Log.Warning($"{message} {GetExceptionText(ex)}");
        public void Warning(string message) => Log.Warning(message);
        public void Warning(Exception ex) => Log.Warning(GetExceptionText(ex));

        public void Error(string message, Exception ex) => Log.Error($"{message} {GetExceptionText(ex)}");
        public void Error(string message) => Log.Error(message);
        public void Error(Exception ex) => Log.Error(GetExceptionText(ex));

        public void Fatal(string message, Exception ex) => Log.Fatal($"{message} {GetExceptionText(ex)}");
        public void Fatal(string message) => Log.Fatal(message);
        public void Fatal(Exception ex) => Log.Fatal(GetExceptionText(ex));


        private string GetExceptionText(Exception exception)
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

        private string GetExceptionText(Exception exception, int level, ref StringBuilder message)
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
