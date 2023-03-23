using static Katzebase.Library.Constants;

namespace Katzebase.Library.Exceptions
{
    public class KatzebaseDuplicateKeyViolation : KatzebaseExceptionBase
    {
        public KatzebaseDuplicateKeyViolation()
        {
            Severity = LogSeverity.Warning;
        }

        public KatzebaseDuplicateKeyViolation(string message)
            : base(message)

        {
            Severity = LogSeverity.Warning;
        }
    }
}