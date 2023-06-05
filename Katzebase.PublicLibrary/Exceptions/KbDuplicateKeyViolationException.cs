using static Katzebase.PublicLibrary.Constants;

namespace Katzebase.PublicLibrary.Exceptions
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