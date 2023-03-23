using static Katzebase.Library.Constants;

namespace Katzebase.Library.Exceptions
{
    public class KatzebaseDuplicateKeyViolationException : KatzebaseExceptionBase
    {
        public KatzebaseDuplicateKeyViolationException()
        {
            Severity = LogSeverity.Warning;
        }

        public KatzebaseDuplicateKeyViolationException(string message)
            : base(message)

        {
            Severity = LogSeverity.Warning;
        }
    }
}