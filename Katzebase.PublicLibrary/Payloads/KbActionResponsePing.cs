namespace Katzebase.PublicLibrary.Payloads
{
    public class KbActionResponsePing : KbActionResponse
    {
        public Guid SessionId { get; set; }
        public ulong ProcessId { get; set; }
        public DateTime ServerTimeUTC { get; set; }
    }
}
