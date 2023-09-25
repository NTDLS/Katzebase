using static NTDLS.Katzebase.KbConstants;

namespace NTDLS.Katzebase.Exceptions
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