using static Katzebase.Library.Constants;

namespace Katzebase.Library.Exceptions
{
    public class KatzebaseSchemaDoesNotExistException : KatzebaseExceptionBase
    {
        public KatzebaseSchemaDoesNotExistException()
        {
            Severity = LogSeverity.Warning;
        }

        public KatzebaseSchemaDoesNotExistException(string message)
            : base(message)

        {
            Severity = LogSeverity.Warning;
        }
    }
}