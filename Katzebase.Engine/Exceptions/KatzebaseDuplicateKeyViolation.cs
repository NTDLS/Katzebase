using static Katzebase.Engine.Constants;

namespace Katzebase.Engine.Exceptions
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