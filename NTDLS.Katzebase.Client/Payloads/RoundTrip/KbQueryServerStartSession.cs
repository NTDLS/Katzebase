using NTDLS.ReliableMessaging;

namespace NTDLS.Katzebase.Client.Payloads.RoundTrip
{
    public class KbQueryServerStartSession : IRmQuery<KbQueryServerStartSessionReply>
    {
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string ClientName { get; set; }

        public KbQueryServerStartSession(string username, string passwordHash, string clientName)
        {
            ClientName = clientName;
            Username = username;
            PasswordHash = passwordHash;
        }
    }

    public class KbQueryServerStartSessionReply : KbBaseActionResponse, IRmQueryReply
    {
        public DateTime? ServerTimeUTC { get; set; }
        public Guid ConnectionId { get; set; }
        public ulong ProcessId { get; set; }
    }
}
