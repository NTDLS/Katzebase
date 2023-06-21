using static Katzebase.PublicLibrary.KbConstants;

namespace Katzebase.PublicLibrary.Exceptions
{
    public class KbSessionNotFoundException : KbExceptionBase
    {
        public KbSessionNotFoundException()
        {
            Severity = KbLogSeverity.Warning;
        }

        public KbSessionNotFoundException(string message)
            : base($"Session not found: {message}.")

        {
            Severity = KbLogSeverity.Exception;
        }
    }
}