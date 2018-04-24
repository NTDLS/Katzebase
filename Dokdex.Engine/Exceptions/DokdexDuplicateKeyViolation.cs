using static Dokdex.Engine.Constants;

namespace Dokdex.Engine.Exceptions
{
    public class DokdexDuplicateKeyViolation : DokdexExceptionBase
    {
        public DokdexDuplicateKeyViolation()
        {
            Severity = LogSeverity.Warning;
        }

        public DokdexDuplicateKeyViolation(string message)
            : base(message)

        {
            Severity = LogSeverity.Warning;
        }
    }
}