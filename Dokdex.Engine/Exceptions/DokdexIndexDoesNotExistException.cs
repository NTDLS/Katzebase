using static Dokdex.Engine.Constants;

namespace Dokdex.Engine.Exceptions
{
    public class DokdexIndexDoesNotExistException : DokdexExceptionBase
    {
        public DokdexIndexDoesNotExistException()
        {
            Severity = LogSeverity.Warning;
        }

        public DokdexIndexDoesNotExistException(string message)
            : base(message)

        {
            Severity = LogSeverity.Warning;
        }
    }
}