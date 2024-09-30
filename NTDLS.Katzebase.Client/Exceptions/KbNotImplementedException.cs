namespace NTDLS.Katzebase.Client.Exceptions
{
    public class KbNotImplementedException : KbExceptionBase
    {
        public KbNotImplementedException()
        {
            Severity = KbConstants.KbLogSeverity.Fatal;
        }

        public KbNotImplementedException(string message)
            : base(message)
        {
            Severity = KbConstants.KbLogSeverity.Fatal;
        }
    }
}
