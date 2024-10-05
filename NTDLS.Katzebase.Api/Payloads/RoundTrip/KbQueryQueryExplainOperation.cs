using NTDLS.Katzebase.Api.Types;
using NTDLS.ReliableMessaging;

namespace NTDLS.Katzebase.Api.Payloads.RoundTrip
{
    public class KbQueryQueryExplainOperation : IRmQuery<KbQueryQueryExplainOperationReply>
    {
        public Guid ConnectionId { get; set; }
        public string Statement { get; set; }
        public KbInsensitiveDictionary<KbConstant>? UserParameters { get; set; }

        public KbQueryQueryExplainOperation(Guid connectionId, string statement, KbInsensitiveDictionary<KbConstant>? userParameters)
        {
            ConnectionId = connectionId;
            Statement = statement;
            UserParameters = userParameters;
        }
    }

    public class KbQueryQueryExplainOperationReply : KbQueryExplainCollection, IRmQueryReply
    {
    }
}
