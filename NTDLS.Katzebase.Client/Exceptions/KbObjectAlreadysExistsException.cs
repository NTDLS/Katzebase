namespace NTDLS.Katzebase.Client.Exceptions
{
    public class KbObjectAlreadyExistsException : KbExceptionBase
    {
        public KbObjectAlreadyExistsException()
        {
        }

        public KbObjectAlreadyExistsException(string? message)
            : base(message)
        {
        }
    }
}
