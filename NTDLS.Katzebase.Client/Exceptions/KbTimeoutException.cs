namespace NTDLS.Katzebase.Client.Exceptions
{
    public class KbTimeoutException : KbExceptionBase
    {
        public KbTimeoutException()
        {
        }

        public KbTimeoutException(string message)
            : base(message)
        {
        }
    }
}
