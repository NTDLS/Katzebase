using static Katzebase.PublicLibrary.Constants;

namespace Katzebase.PublicLibrary.Exceptions
{
    public class KbTransactionCancelledException : KbExceptionBase
    {
        public KbTransactionCancelledException()
        {
            Severity = LogSeverity.Warning;
        }

        public KbTransactionCancelledException(string message)
            : base($"Transaction Cancelled: {message}.")

        {
            Severity = LogSeverity.Exception;
        }
    }
}