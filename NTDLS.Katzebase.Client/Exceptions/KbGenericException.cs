namespace NTDLS.Katzebase.Client.Exceptions
{
    public class KbGenericException : KbExceptionBase
    {
        public KbGenericException()
        {
        }

        public KbGenericException(string? message)
            : base(message)
        {
        }
    }
}
