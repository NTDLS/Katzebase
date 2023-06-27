using static Katzebase.PublicLibrary.KbConstants;

namespace Katzebase.PublicLibrary.Exceptions
{
    public class KbEngineException : KbExceptionBase
    {
        public KbEngineException()
        {
            Severity = KbLogSeverity.Warning;
        }

        public KbEngineException(string message)
            : base($"Engine exception: {message}.")

        {
            Severity = KbLogSeverity.Warning;
        }
    }
}
