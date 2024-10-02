namespace NTDLS.Katzebase.Client.Exceptions
{
    /// <summary>
    /// Used to report unexpected engine operations.
    /// </summary>
    public class KbEngineException : KbExceptionBase
    {
        public KbEngineException()
        {
            Severity = KbConstants.KbLogSeverity.Error;
        }

        public KbEngineException(string message)
            : base(message)
        {
            Severity = KbConstants.KbLogSeverity.Error;
        }
    }
}
