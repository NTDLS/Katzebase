namespace NTDLS.Katzebase.Client.Exceptions
{
    public class KbInvalidArgumentException : KbExceptionBase
    {
        public KbInvalidArgumentException()
        {
            Severity = KbConstants.KbLogSeverity.Verbose;
        }

        public KbInvalidArgumentException(string message)
            : base(message)
        {
            Severity = KbConstants.KbLogSeverity.Verbose;
        }
    }
}
