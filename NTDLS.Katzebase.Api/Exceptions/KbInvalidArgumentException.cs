namespace NTDLS.Katzebase.Api.Exceptions
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
