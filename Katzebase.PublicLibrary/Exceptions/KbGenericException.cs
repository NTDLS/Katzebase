using static Katzebase.PublicLibrary.Constants;

namespace Katzebase.PublicLibrary.Exceptions
{
    public class KbGenericException : KbExceptionBase
    {
        public KbGenericException()
        {
            Severity = LogSeverity.Warning;
        }

        public KbGenericException(string ?message)
            : base($"Generic exception: {message}.")

        {
            Severity = LogSeverity.Exception;
        }
    }
}