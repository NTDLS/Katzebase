using static Katzebase.Engine.Constants;

namespace Katzebase.Engine.Exceptions
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