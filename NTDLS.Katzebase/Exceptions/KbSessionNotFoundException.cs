using static Katzebase.KbConstants;

namespace Katzebase.Exceptions
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