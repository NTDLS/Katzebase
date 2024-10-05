using static NTDLS.Katzebase.Api.KbConstants;

namespace NTDLS.Katzebase.Api.Exceptions
{
    public class KbExceptionBase : Exception
    {
        public KbLogSeverity Severity { get; set; } = KbLogSeverity.Debug;

        public KbExceptionBase()
        {
        }

        public KbExceptionBase(string? message)
            : base(message)
        {
        }
    }
}
