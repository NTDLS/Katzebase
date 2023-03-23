using static Katzebase.Library.Constants;

namespace Katzebase.Library.Exceptions
{
    public class KatzebaseCriticalNull : KatzebaseExceptionBase
    {
        public KatzebaseCriticalNull()
        {
            Severity = LogSeverity.Warning;
        }

        public KatzebaseCriticalNull(string message)
            : base(message)

        {
            Severity = LogSeverity.Warning;
        }
    }
}