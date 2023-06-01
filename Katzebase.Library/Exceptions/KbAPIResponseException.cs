using static Katzebase.Library.Constants;

namespace Katzebase.Library.Exceptions
{
    public class KbAPIResponseException : KbExceptionBase
    {
        public KbAPIResponseException()
        {
            Severity = LogSeverity.Warning;
        }

        public KbAPIResponseException(string? message)
            : base(message)

        {
            Severity = LogSeverity.Exception;
        }
    }
}