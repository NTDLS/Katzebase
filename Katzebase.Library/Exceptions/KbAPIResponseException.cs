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
            : base($"API exception: {message}.")

        {
            Severity = LogSeverity.Exception;
        }
    }
}