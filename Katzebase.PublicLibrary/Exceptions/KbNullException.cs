using static Katzebase.PublicLibrary.KbConstants;

namespace Katzebase.PublicLibrary.Exceptions
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