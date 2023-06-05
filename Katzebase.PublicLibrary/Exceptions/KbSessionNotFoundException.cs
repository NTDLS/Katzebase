using static Katzebase.PublicLibrary.Constants;

namespace Katzebase.PublicLibrary.Exceptions
{
    public class KbSessionNotFoundException : KbExceptionBase
    {
        public KbSessionNotFoundException()
        {
            Severity = LogSeverity.Warning;
        }

        public KbSessionNotFoundException(string message)
            : base($"Session not found: {message}.")

        {
            Severity = LogSeverity.Exception;
        }
    }
}