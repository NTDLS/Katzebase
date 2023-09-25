using static NTDLS.Katzebase.KbConstants;

namespace NTDLS.Katzebase.Exceptions
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