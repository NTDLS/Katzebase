using static NTDLS.Katzebase.Client.KbConstants;

namespace NTDLS.Katzebase.Client.Exceptions
{
    public class KbDeadlockException : KbExceptionBase
    {
        public KbDeadlockException()
        {
            Severity = KbLogSeverity.Warning;
        }

        public KbDeadlockException(string message)
            : base($"Deadlock exception: {message}.")

        {
            Severity = KbLogSeverity.Warning;
        }

        public KbDeadlockException(string message, string explanation)
            : base($"Deadlock exception: {message}.\r\n\r\nExplanation:\r\n{explanation}")
        {
            Severity = KbLogSeverity.Warning;
        }
    }
}