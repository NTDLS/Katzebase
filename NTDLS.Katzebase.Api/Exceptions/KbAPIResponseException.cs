namespace NTDLS.Katzebase.Api.Exceptions
{
    public class KbAPIResponseException : KbExceptionBase
    {
        public KbAPIResponseException()
        {
            Severity = KbConstants.KbLogSeverity.Error;
        }

        public KbAPIResponseException(string? message)
            : base(message)
        {
            Severity = KbConstants.KbLogSeverity.Error;
        }
    }
}
