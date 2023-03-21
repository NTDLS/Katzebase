using static Katzebase.Engine.Constants;

namespace Katzebase.Engine.Exceptions
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