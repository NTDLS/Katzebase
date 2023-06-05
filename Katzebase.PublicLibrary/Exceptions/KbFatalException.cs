using static Katzebase.PublicLibrary.Constants;

namespace Katzebase.PublicLibrary.Exceptions
{
    public class KbFatalException : KbExceptionBase
    {
        public KbFatalException()
        {
            Severity = LogSeverity.Warning;
        }

        public KbFatalException(string? message)
            : base($"Fatal exception: {message}.")

        {
            Severity = LogSeverity.Exception;
        }
    }
}