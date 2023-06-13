using static Katzebase.PublicLibrary.Constants;

namespace Katzebase.PublicLibrary.Exceptions
{
    public class KbObjectAlreadysExistsException : KbExceptionBase
    {
        public KbObjectAlreadysExistsException()
        {
            Severity = LogSeverity.Warning;
        }

        public KbObjectAlreadysExistsException(string? message)
            : base($"Object already exists exception: {message}.")

        {
            Severity = LogSeverity.Exception;
        }
    }
}