using static Katzebase.Engine.Constants;

namespace Katzebase.Engine.Exceptions
{
    public class KatzebaseExceptionBase : Exception
    {
        public LogSeverity Severity { get; set; }

        public KatzebaseExceptionBase()
        {
            Severity = LogSeverity.Exception;
        }

        public KatzebaseExceptionBase(string message)
            : base(message)

        {
            Severity = LogSeverity.Exception;
        }
    }
}
