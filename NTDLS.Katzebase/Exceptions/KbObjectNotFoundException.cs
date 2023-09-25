using static NTDLS.Katzebase.KbConstants;

namespace NTDLS.Katzebase.Exceptions
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