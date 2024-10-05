namespace NTDLS.Katzebase.Api.Exceptions
{
    public class KbObjectNotFoundException : KbExceptionBase
    {
        public KbObjectNotFoundException()
        {
            Severity = KbConstants.KbLogSeverity.Verbose;
        }

        public KbObjectNotFoundException(string? message)
            : base(message)
        {
            Severity = KbConstants.KbLogSeverity.Verbose;
        }
    }
}
