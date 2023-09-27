using static NTDLS.Katzebase.Client.KbConstants;

namespace NTDLS.Katzebase.Client.Exceptions
{
    public class KbTransactionCancelledException : KbExceptionBase
    {
        public KbTransactionCancelledException()
        {
            Severity = KbLogSeverity.Warning;
        }

        public KbTransactionCancelledException(string message)
            : base($"Transaction Cancelled: {message}.")

        {
            Severity = KbLogSeverity.Exception;
        }
    }
}