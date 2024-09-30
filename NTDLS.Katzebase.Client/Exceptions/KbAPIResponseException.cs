namespace NTDLS.Katzebase.Client.Exceptions
{
    public class KbAPIResponseException : KbExceptionBase
    {
        public KbAPIResponseException()
        {
        }

        public KbAPIResponseException(string? message)
            : base(message)
        {
        }
    }
}
