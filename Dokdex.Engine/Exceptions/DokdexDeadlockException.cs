using static Dokdex.Engine.Constants;

namespace Dokdex.Engine.Exceptions
{
    public class DokdexDeadlockException : DokdexExceptionBase
    {
        public DokdexDeadlockException()
        {
            Severity = LogSeverity.Warning;
        }

        public DokdexDeadlockException(string message)
            : base(message)

        {
            Severity = LogSeverity.Warning;
        }
    }
}