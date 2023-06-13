using static Katzebase.PublicLibrary.Constants;

namespace Katzebase.PublicLibrary.Exceptions
{
    public class KbObjectNotFoundException : KbExceptionBase
    {
        public KbObjectNotFoundException()
        {
            Severity = LogSeverity.Warning;
        }

        public KbObjectNotFoundException(string? message)
            : base($"Object not found exception: {message}.")

        {
            Severity = LogSeverity.Exception;
        }
    }
}