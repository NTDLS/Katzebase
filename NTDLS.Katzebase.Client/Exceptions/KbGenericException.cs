using static NTDLS.Katzebase.Client.KbConstants;

namespace NTDLS.Katzebase.Client.Exceptions
{
    public class KbGenericException : KbExceptionBase
    {
        public KbGenericException()
        {
            Severity = KbLogSeverity.Warning;
        }

        public KbGenericException(string? message)
            : base($"Generic exception: {message}.")

        {
            Severity = KbLogSeverity.Exception;
        }
    }
}