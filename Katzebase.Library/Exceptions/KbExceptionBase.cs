using static Katzebase.Library.Constants;

namespace Katzebase.Library.Exceptions
{
    public class KbExceptionBase : Exception
    {
        public LogSeverity Severity { get; set; }

        public KbExceptionBase()
        {
            Severity = LogSeverity.Exception;
        }

        public KbExceptionBase(string message)
            : base(message)

        {
            Severity = LogSeverity.Exception;
        }
    }
}
