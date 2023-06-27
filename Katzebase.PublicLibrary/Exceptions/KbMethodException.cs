using static Katzebase.PublicLibrary.KbConstants;

namespace Katzebase.PublicLibrary.Exceptions
{
    public class KbMethodException : KbExceptionBase
    {
        public KbMethodException()
        {
            Severity = KbLogSeverity.Warning;
        }

        public KbMethodException(string message)
            : base($"Method exception: {message}.")

        {
            Severity = KbLogSeverity.Warning;
        }
    }
}