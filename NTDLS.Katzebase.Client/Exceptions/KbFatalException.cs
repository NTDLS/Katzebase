using static NTDLS.Katzebase.Client.KbConstants;

namespace NTDLS.Katzebase.Client.Exceptions
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