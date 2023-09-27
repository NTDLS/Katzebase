using static NTDLS.Katzebase.Client.KbConstants;

namespace NTDLS.Katzebase.Client.Exceptions
{
    public class KbObjectNotFoundException : KbExceptionBase
    {
        public KbObjectNotFoundException()
        {
            Severity = KbLogSeverity.Warning;
        }

        public KbObjectNotFoundException(string? message)
            : base($"Object not found exception: {message}.")

        {
            Severity = KbLogSeverity.Exception;
        }
    }
}