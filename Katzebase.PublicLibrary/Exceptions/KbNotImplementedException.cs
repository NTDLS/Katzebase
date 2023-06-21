using static Katzebase.PublicLibrary.KbConstants;

namespace Katzebase.PublicLibrary.Exceptions
{
    public class KbNotImplementedException : KbExceptionBase
    {
        public KbNotImplementedException()
        {
            Severity = KbLogSeverity.Warning;
        }

        public KbNotImplementedException(string message)
            : base($"Not implemented exception: {message}.")

        {
            Severity = KbLogSeverity.Warning;
        }
    }
}