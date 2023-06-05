using static Katzebase.PublicLibrary.Constants;

namespace Katzebase.PublicLibrary.Exceptions
{
    public class KbNotImplementedException : KbExceptionBase
    {
        public KbNotImplementedException()
        {
            Severity = LogSeverity.Warning;
        }

        public KbNotImplementedException(string message)
            : base($"Not implemented exception: {message}.")

        {
            Severity = LogSeverity.Warning;
        }
    }
}