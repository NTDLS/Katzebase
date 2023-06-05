using static Katzebase.PublicLibrary.Constants;

namespace Katzebase.Engine.Logging
{
    public class LogEntry
    {
        public DateTime DateTime { get; set; }
        public string? Message { get; set; }
        public Exception? Exception { get; set; }
        public LogSeverity? Severity { get; set; }

        public LogEntry()
        {
            DateTime = DateTime.Now;
        }

        public LogEntry(string message)
        {
            DateTime = DateTime.Now;
            Message = message;
        }
    }
}
