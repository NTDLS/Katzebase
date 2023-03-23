using static Katzebase.Library.Constants;

namespace Katzebase.Library.Exceptions
{
    public class KatzebaseIndexDoesNotExistException : KatzebaseExceptionBase
    {
        public KatzebaseIndexDoesNotExistException()
        {
            Severity = LogSeverity.Warning;
        }

        public KatzebaseIndexDoesNotExistException(string message)
            : base(message)

        {
            Severity = LogSeverity.Warning;
        }
    }
}