using static Katzebase.Library.Constants;

namespace Katzebase.Library.Exceptions
{
    public class KbIndexDoesNotExistException : KbExceptionBase
    {
        public KbIndexDoesNotExistException()
        {
            Severity = LogSeverity.Warning;
        }

        public KbIndexDoesNotExistException(string message)
            : base(message)

        {
            Severity = LogSeverity.Warning;
        }
    }
}