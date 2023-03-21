using static Katzebase.Engine.Constants;

namespace Katzebase.Engine.Exceptions
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