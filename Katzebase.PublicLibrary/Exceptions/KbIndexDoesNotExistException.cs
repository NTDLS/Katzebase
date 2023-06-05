using static Katzebase.PublicLibrary.Constants;

namespace Katzebase.PublicLibrary.Exceptions
{
    public class KbIndexDoesNotExistException : KbExceptionBase
    {
        public KbIndexDoesNotExistException()
        {
            Severity = LogSeverity.Warning;
        }

        public KbIndexDoesNotExistException(string message)
            : base($"Index does not exist exception: {message}.")

        {
            Severity = LogSeverity.Warning;
        }
    }
}