namespace NTDLS.Katzebase.Client.Exceptions
{
    public class KbMultipleRecordSetsException : KbExceptionBase
    {
        public KbMultipleRecordSetsException()
        {
            Severity = KbConstants.KbLogSeverity.Verbose;
        }

        public KbMultipleRecordSetsException(string? message)
            : base(message)
        {
            Severity = KbConstants.KbLogSeverity.Verbose;
        }
    }
}
