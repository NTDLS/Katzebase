namespace NTDLS.Katzebase.Client.Exceptions
{
    /// <summary>
    /// Used to report errors that occur when processing a query.
    /// </summary>
    public class KbProcessingException : KbExceptionBase
    {
        public KbProcessingException()
        {
            Severity = KbConstants.KbLogSeverity.Verbose;
        }

        public KbProcessingException(string message)
            : base(message)
        {
            Severity = KbConstants.KbLogSeverity.Verbose;
        }
    }
}
