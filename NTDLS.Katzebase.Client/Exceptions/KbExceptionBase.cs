using static NTDLS.Katzebase.Client.KbConstants;

namespace NTDLS.Katzebase.Client.Exceptions
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
