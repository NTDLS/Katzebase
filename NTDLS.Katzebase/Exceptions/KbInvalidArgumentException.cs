using static Katzebase.KbConstants;

namespace Katzebase.Exceptions
{
    public class KbInvalidArgumentException : KbExceptionBase
    {
        public KbInvalidArgumentException()
        {
            Severity = KbLogSeverity.Warning;
        }

        public KbInvalidArgumentException(string message)
            : base($"Invalid argument exception: {message}.")

        {
            Severity = KbLogSeverity.Warning;
        }
    }
}