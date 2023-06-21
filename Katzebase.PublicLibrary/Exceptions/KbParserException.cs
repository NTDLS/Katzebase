using static Katzebase.PublicLibrary.KbConstants;

namespace Katzebase.PublicLibrary.Exceptions
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
