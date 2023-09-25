using static NTDLS.Katzebase.KbConstants;

namespace NTDLS.Katzebase.Exceptions
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