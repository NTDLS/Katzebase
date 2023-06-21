using static Katzebase.PublicLibrary.KbConstants;

namespace Katzebase.PublicLibrary.Exceptions
{
    public class KbFatalException : KbExceptionBase
    {
        public KbFatalException()
        {
            Severity = KbLogSeverity.Warning;
        }

        public KbFatalException(string? message)
            : base($"Fatal exception: {message}.")

        {
            Severity = KbLogSeverity.Exception;
        }
    }
}