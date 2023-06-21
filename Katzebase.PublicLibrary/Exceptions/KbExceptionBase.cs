using static Katzebase.PublicLibrary.KbConstants;

namespace Katzebase.PublicLibrary.Exceptions
{
    public class KbExceptionBase : Exception
    {
        public KbLogSeverity Severity { get; set; }

        public KbExceptionBase()
        {
            Severity = KbLogSeverity.Exception;
        }

        public KbExceptionBase(string? message)
            : base(message)

        {
            Severity = KbLogSeverity.Exception;
        }
    }
}
