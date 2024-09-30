namespace NTDLS.Katzebase.Client.Exceptions
{
    public class KbObjectNotFoundException : KbExceptionBase
    {
        public KbObjectNotFoundException()
        {
        }

        public KbObjectNotFoundException(string? message)
            : base(message)
        {
        }
    }
}
