using static Katzebase.KbConstants;

namespace Katzebase.Exceptions
{
    public class KbDuplicateKeyViolationException : KbExceptionBase
    {
        public KbDuplicateKeyViolationException()
        {
            Severity = KbLogSeverity.Warning;
        }

        public KbDuplicateKeyViolationException(string message)
            : base($"Duplicate key violation exception: {message}.")

        {
            Severity = KbLogSeverity.Warning;
        }
    }
}