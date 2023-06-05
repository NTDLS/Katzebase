using static Katzebase.PublicLibrary.Constants;

namespace Katzebase.PublicLibrary.Exceptions
{
    public class KbParserException : KbExceptionBase
    {
        public KbParserException()
        {
            Severity = LogSeverity.Warning;
        }

        public KbParserException(string message)
            : base($"Parser exception: {message}.")

        {
            Severity = LogSeverity.Warning;
        }
    }
}
