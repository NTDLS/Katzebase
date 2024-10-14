namespace NTDLS.Katzebase.Api.Exceptions
{
    public class KbPermissionNotHeld : KbExceptionBase
    {
        public KbPermissionNotHeld()
        {
            Severity = KbConstants.KbLogSeverity.Verbose;
        }

        public KbPermissionNotHeld(string? message)
            : base(message)
        {
            Severity = KbConstants.KbLogSeverity.Verbose;
        }
    }
}
