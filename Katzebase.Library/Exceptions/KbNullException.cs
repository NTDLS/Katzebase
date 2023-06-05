using static Katzebase.Library.Constants;

namespace Katzebase.Library.Exceptions
{
    public class KbNullException : KbExceptionBase
    {
        public KbNullException()
        {
            Severity = LogSeverity.Warning;
        }

        public KbNullException(string message)
            : base($"Null exception: {message}.")

        {
            Severity = LogSeverity.Exception;
        }
    }
}