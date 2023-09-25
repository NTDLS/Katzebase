using static NTDLS.Katzebase.KbConstants;

namespace NTDLS.Katzebase.Exceptions
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
