using static NTDLS.Katzebase.Client.KbConstants;

namespace NTDLS.Katzebase.Client.Exceptions
{
    public class KbTimeoutException : KbExceptionBase
    {
        public KbTimeoutException()
        {
            Severity = KbLogSeverity.Warning;
        }

        public KbTimeoutException(string message)
            : base($"Function exception: {message}.")

        {
            Severity = KbLogSeverity.Warning;
        }
    }
}