using static Katzebase.Library.Constants;

namespace Katzebase.Library.Exceptions
{
    public class KbSchemaDoesNotExistException : KbExceptionBase
    {
        public KbSchemaDoesNotExistException()
        {
            Severity = LogSeverity.Warning;
        }

        public KbSchemaDoesNotExistException(string message)
            : base(message)

        {
            Severity = LogSeverity.Warning;
        }
    }
}