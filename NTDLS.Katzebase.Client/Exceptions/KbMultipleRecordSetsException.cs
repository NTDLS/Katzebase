namespace NTDLS.Katzebase.Client.Exceptions
{
    public class KbMultipleRecordSetsException : KbExceptionBase
    {
        public KbMultipleRecordSetsException()
        {
        }

        public KbMultipleRecordSetsException(string? message)
            : base(message)
        {
        }
    }
}
