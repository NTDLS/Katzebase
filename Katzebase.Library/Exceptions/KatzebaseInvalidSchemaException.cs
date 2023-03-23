using static Katzebase.Library.Constants;

namespace Katzebase.Library.Exceptions
{
    public class KatzebaseInvalidSchemaException : KatzebaseExceptionBase
    {
        public KatzebaseInvalidSchemaException()
        {
            Severity = LogSeverity.Warning;
        }

        public KatzebaseInvalidSchemaException(string message)
            : base(message)

        {
            Severity = LogSeverity.Warning;
        }
    }
}