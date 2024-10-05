using NTDLS.ReliableMessaging;

namespace NTDLS.Katzebase.Api.Payloads.RoundTrip
{
    public class KbQueryProcedureExecute : IRmQuery<KbQueryProcedureExecuteReply>
    {
        public Guid ConnectionId { get; set; }
        public KbProcedure Procedure { get; set; }

        public KbQueryProcedureExecute(Guid connectionId, KbProcedure procedure)
        {
            ConnectionId = connectionId;
            Procedure = procedure;
        }
    }

    public class KbQueryProcedureExecuteReply : KbQueryResultCollection, IRmQueryReply
    {
    }
}
