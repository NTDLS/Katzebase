using NTDLS.Katzebase.Api.Types;
using NTDLS.ReliableMessaging;

namespace NTDLS.Katzebase.Api.Payloads.RoundTrip
{
    public class KbQueryQueryExplainPlan : IRmQuery<KbQueryQueryExplainPlanReply>
    {
        public Guid ConnectionId { get; set; }
        public string Statement { get; set; }
        public KbInsensitiveDictionary<KbConstant>? UserParameters { get; set; }

        public KbQueryQueryExplainPlan(Guid connectionId, string statement, KbInsensitiveDictionary<KbConstant>? userParameters)
        {
            ConnectionId = connectionId;
            Statement = statement;
            UserParameters = userParameters;
        }
    }

    public class KbQueryQueryExplainPlanReply : KbQueryExplainCollection, IRmQueryReply
    {
    }
}
