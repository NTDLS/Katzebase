using static Katzebase.KbConstants;

namespace Katzebase.Exceptions
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
