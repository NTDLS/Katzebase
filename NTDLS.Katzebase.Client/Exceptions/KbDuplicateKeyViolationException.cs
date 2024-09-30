namespace NTDLS.Katzebase.Client.Exceptions
{
    public class KbDuplicateKeyViolationException : KbExceptionBase
    {
        public KbDuplicateKeyViolationException()
        {
        }

        public KbDuplicateKeyViolationException(string message)
            : base(message)
        {
        }
    }
}
