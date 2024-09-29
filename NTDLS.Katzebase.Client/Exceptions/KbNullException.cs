namespace NTDLS.Katzebase.Client.Exceptions
{
    public class KbNullException : KbExceptionBase
    {
        public KbNullException()
        {
        }

        public KbNullException(string message)
            : base(message)
        {
        }
    }
}
