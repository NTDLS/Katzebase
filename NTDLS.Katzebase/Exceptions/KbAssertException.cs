using static NTDLS.Katzebase.KbConstants;

namespace NTDLS.Katzebase.Exceptions
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