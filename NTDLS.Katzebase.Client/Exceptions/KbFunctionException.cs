using static NTDLS.Katzebase.Client.KbConstants;

namespace NTDLS.Katzebase.Client.Exceptions
{
    public class KbFunctionException : KbExceptionBase
    {
        public KbFunctionException()
        {
            Severity = KbLogSeverity.Warning;
        }

        public KbFunctionException(string message)
            : base($"Function exception: {message}.")

        {
            Severity = KbLogSeverity.Warning;
        }
    }
}