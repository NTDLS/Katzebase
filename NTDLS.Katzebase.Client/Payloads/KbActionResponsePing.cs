namespace NTDLS.Katzebase.Client.Payloads
{
    public class KbActionResponsePing : KbBaseActionResponse
    {
        public Guid SessionId { get; set; }
        public ulong ProcessId { get; set; }
        public DateTime ServerTimeUTC { get; set; }
    }
}
