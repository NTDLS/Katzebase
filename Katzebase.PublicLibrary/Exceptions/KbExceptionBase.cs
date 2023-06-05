using static Katzebase.PublicLibrary.Constants;

namespace Katzebase.PublicLibrary.Exceptions
{
    public class KbExceptionBase : Exception
    {
        public LogSeverity Severity { get; set; }

        public KbExceptionBase()
        {
            Severity = LogSeverity.Exception;
        }

        public KbExceptionBase(string? message)
            : base(message)

        {
            Severity = LogSeverity.Exception;
        }
    }
}
