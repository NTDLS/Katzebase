using static Katzebase.Library.Constants;

namespace Katzebase.Library.Exceptions
{
    public class KbssertException : KbExceptionBase
    {
        public KbssertException()
        {
            Severity = LogSeverity.Warning;
        }

        public KbssertException(string message)
            : base(message)

        {
            Severity = LogSeverity.Exception;
        }
    }
}