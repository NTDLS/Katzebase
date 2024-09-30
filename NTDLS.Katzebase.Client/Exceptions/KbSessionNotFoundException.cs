namespace NTDLS.Katzebase.Client.Exceptions
{
    public class KbSessionNotFoundException : KbExceptionBase
    {
        public KbSessionNotFoundException()
        {
            Severity = KbConstants.KbLogSeverity.Warning;
        }

        public KbSessionNotFoundException(string message)
            : base(message)
        {
            Severity = KbConstants.KbLogSeverity.Warning;
        }
    }
}
