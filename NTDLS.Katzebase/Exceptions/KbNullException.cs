using static Katzebase.KbConstants;

namespace Katzebase.Exceptions
{
    public class KbNullException : KbExceptionBase
    {
        public KbNullException()
        {
            Severity = KbLogSeverity.Warning;
        }

        public KbNullException(string message)
            : base($"Null exception: {message}.")

        {
            Severity = KbLogSeverity.Exception;
        }
    }
}