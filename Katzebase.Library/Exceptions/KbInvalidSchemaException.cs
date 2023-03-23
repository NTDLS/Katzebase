using static Katzebase.Library.Constants;

namespace Katzebase.Library.Exceptions
{
    public class KbInvalidSchemaException : KbExceptionBase
    {
        public KbInvalidSchemaException()
        {
            Severity = LogSeverity.Warning;
        }

        public KbInvalidSchemaException(string message)
            : base(message)

        {
            Severity = LogSeverity.Warning;
        }
    }
}