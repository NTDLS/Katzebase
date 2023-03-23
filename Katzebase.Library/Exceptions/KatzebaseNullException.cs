using static Katzebase.Library.Constants;

namespace Katzebase.Library.Exceptions
{
    public class KatzebaseNullException : KatzebaseExceptionBase
    {
        public KatzebaseNullException()
        {
            Severity = LogSeverity.Warning;
        }

        public KatzebaseNullException(string message)
            : base(message)

        {
            Severity = LogSeverity.Warning;
        }
    }
}