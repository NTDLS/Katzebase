using NTDLS.Katzebase.Client.Types;
using NTDLS.ReliableMessaging;

namespace NTDLS.Katzebase.Client.Payloads.RoundTrip
{
    public class KbQueryQueryExplainOperations : IRmQuery<KbQueryQueryExplainOperationsReply>
    {
        public Guid ConnectionId { get; set; }
        public List<string> Statements { get; set; }
        public KbInsensitiveDictionary<KbConstant>? UserParameters { get; set; }

        public KbQueryQueryExplainOperations(Guid connectionId, List<string> statements, KbInsensitiveDictionary<KbConstant>? userParameters)
        {
            ConnectionId = connectionId;
            Statements = statements;
            UserParameters = userParameters;
        }
    }

    public class KbQueryQueryExplainOperationsReply : KbQueryExplainCollection, IRmQueryReply
    {
    }
}
