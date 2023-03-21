using static Katzebase.Engine.Constants;

namespace Katzebase.Engine.Exceptions
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