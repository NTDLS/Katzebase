using static Katzebase.PublicLibrary.KbConstants;

namespace Katzebase.PublicLibrary.Exceptions
{
    public class KbAPIResponseException : KbExceptionBase
    {
        public KbAPIResponseException()
        {
            Severity = KbLogSeverity.Warning;
        }

        public KbAPIResponseException(string? message)
            : base($"API exception: {message}.")

        {
            Severity = KbLogSeverity.Exception;
        }
    }
}