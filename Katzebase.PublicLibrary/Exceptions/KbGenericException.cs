using static Katzebase.PublicLibrary.KbConstants;

namespace Katzebase.PublicLibrary.Exceptions
{
    public class KbGenericException : KbExceptionBase
    {
        public KbGenericException()
        {
            Severity = KbLogSeverity.Warning;
        }

        public KbGenericException(string? message)
            : base($"Generic exception: {message}.")

        {
            Severity = KbLogSeverity.Exception;
        }
    }
}