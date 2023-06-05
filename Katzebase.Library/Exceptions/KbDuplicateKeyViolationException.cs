using static Katzebase.Library.Constants;

namespace Katzebase.Library.Exceptions
{
    public class KbDuplicateKeyViolationException : KbExceptionBase
    {
        public KbDuplicateKeyViolationException()
        {
            Severity = LogSeverity.Warning;
        }

        public KbDuplicateKeyViolationException(string message)
            : base($"Duplicate key violation exception: {message}.")

        {
            Severity = LogSeverity.Warning;
        }
    }
}