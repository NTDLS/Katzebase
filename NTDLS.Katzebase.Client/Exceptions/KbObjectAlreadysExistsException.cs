namespace NTDLS.Katzebase.Client.Exceptions
{
    public class KbObjectAlreadyExistsException : KbExceptionBase
    {
        public KbObjectAlreadyExistsException()
        {
            Severity = KbConstants.KbLogSeverity.Verbose;
        }

        public KbObjectAlreadyExistsException(string? message)
            : base(message)
        {
            Severity = KbConstants.KbLogSeverity.Verbose;
        }
    }
}
