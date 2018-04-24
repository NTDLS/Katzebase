using static Dokdex.Engine.Constants;

namespace Dokdex.Engine.Exceptions
{
    public class DokdexSchemaDoesNotExistException : DokdexExceptionBase
    {
        public DokdexSchemaDoesNotExistException()
        {
            Severity = LogSeverity.Warning;
        }

        public DokdexSchemaDoesNotExistException(string message)
            : base(message)

        {
            Severity = LogSeverity.Warning;
        }
    }
}