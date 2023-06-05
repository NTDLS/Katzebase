using static Katzebase.PublicLibrary.Constants;

namespace Katzebase.PublicLibrary.Exceptions
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