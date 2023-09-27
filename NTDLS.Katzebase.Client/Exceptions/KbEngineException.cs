using static NTDLS.Katzebase.Client.KbConstants;

namespace NTDLS.Katzebase.Client.Exceptions
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
