using static Dokdex.Engine.Constants;

namespace Dokdex.Engine.Exceptions
{
    public class DokdexInvalidSchemaException : DokdexExceptionBase
    {
        public DokdexInvalidSchemaException()
        {
            Severity = LogSeverity.Warning;
        }

        public DokdexInvalidSchemaException(string message)
            : base(message)

        {
            Severity = LogSeverity.Warning;
        }
    }
}