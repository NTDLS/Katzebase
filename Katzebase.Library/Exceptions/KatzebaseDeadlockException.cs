using static Katzebase.Library.Constants;

namespace Katzebase.Library.Exceptions
{
    public class KatzebaseDeadlockException : KatzebaseExceptionBase
    {
        public KatzebaseDeadlockException()
        {
            Severity = LogSeverity.Warning;
        }

        public KatzebaseDeadlockException(string message)
            : base(message)

        {
            Severity = LogSeverity.Warning;
        }
    }
}