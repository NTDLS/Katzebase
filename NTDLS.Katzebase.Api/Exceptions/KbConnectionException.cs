namespace NTDLS.Katzebase.Api.Exceptions
{
    /// <summary>
    /// Thrown when a request cannot be made because the client is not connected to the server, or the connection is lost
    /// while waiting on a reply. When the connection is lost, the server rolls back the session's open transaction.
    /// </summary>
    public class KbConnectionException : KbExceptionBase
    {
        public KbConnectionException()
        {
            Severity = KbConstants.KbLogSeverity.Error;
        }

        public KbConnectionException(string? message)
            : base(message)
        {
            Severity = KbConstants.KbLogSeverity.Error;
        }

        public KbConnectionException(string? message, Exception? innerException)
            : base(message, innerException)
        {
            Severity = KbConstants.KbLogSeverity.Error;
        }
    }
}
