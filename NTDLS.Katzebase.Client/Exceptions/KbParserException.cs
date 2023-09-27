using static NTDLS.Katzebase.Client.KbConstants;

namespace NTDLS.Katzebase.Client.Exceptions
{
    public class KbParserException : KbExceptionBase
    {
        public KbParserException()
        {
            Severity = KbLogSeverity.Warning;
        }

        public KbParserException(string message)
            : base($"Parser exception: {message}.")

        {
            Severity = KbLogSeverity.Warning;
        }
    }
}
