namespace NTDLS.Katzebase.Client.Exceptions
{
    public class KbAssertException : KbExceptionBase
    {
        public KbAssertException()
        {
            Severity = KbConstants.KbLogSeverity.Error;
        }

        public KbAssertException(string message)
            : base(message)
        {
            Severity = KbConstants.KbLogSeverity.Error;
        }
    }
}
