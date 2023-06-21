using static Katzebase.PublicLibrary.KbConstants;

namespace Katzebase.PublicLibrary.Exceptions
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