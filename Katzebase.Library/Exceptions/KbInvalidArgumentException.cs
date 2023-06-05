using static Katzebase.Library.Constants;

namespace Katzebase.Library.Exceptions
{
    public class KbInvalidArgumentException : KbExceptionBase
    {
        public KbInvalidArgumentException()
        {
            Severity = LogSeverity.Warning;
        }

        public KbInvalidArgumentException(string message)
            : base($"Invalid argument exception: {message}.")

        {
            Severity = LogSeverity.Warning;
        }
    }
}