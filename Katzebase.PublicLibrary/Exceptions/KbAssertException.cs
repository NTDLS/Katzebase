using static Katzebase.PublicLibrary.KbConstants;

namespace Katzebase.PublicLibrary.Exceptions
{
    public class KbAssertException : KbExceptionBase
    {
        public KbAssertException()
        {
            Severity = KbLogSeverity.Warning;
        }

        public KbAssertException(string message)
            : base($"Assert exception: {message}.")

        {
            Severity = KbLogSeverity.Exception;
        }
    }
}