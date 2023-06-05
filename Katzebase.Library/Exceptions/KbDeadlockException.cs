using static Katzebase.Library.Constants;

namespace Katzebase.Library.Exceptions
{
    public class KbDeadlockException : KbExceptionBase
    {
        public KbDeadlockException()
        {
            Severity = LogSeverity.Warning;
        }

        public KbDeadlockException(string message)
            : base($"Deadlock exception: {message}.")

        {
            Severity = LogSeverity.Warning;
        }
    }
}