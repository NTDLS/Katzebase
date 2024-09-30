using NTDLS.ReliableMessaging;
using System.Xml.Linq;

namespace NTDLS.Katzebase.Client.Payloads.RoundTrip
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
    public class KbQueryProcedureExecute<TData> : IRmQuery<KbQueryProcedureExecuteReply<TData>>
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
    public class KbQueryProcedureExecuteReply<TData> : KbQueryResultCollection<TData>, IRmQueryReply
    {
    }
}
