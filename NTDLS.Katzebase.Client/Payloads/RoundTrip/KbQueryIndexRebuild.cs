using NTDLS.ReliableMessaging;

namespace NTDLS.Katzebase.Client.Payloads.RoundTrip
{
    public class KbQueryIndexRebuild : IRmQuery<KbQueryIndexRebuildReply>
    {
        public Guid ConnectionId { get; set; }
        public string Schema { get; set; }
        public string IndexName { get; set; }
        public uint NewPartitionCount { get; set; }

        public KbQueryIndexRebuild(Guid connectionId, string schema, string indexName, uint newPartitionCount)
        {
            ConnectionId = connectionId;
            Schema = schema;
            IndexName = indexName;
            NewPartitionCount = newPartitionCount;
        }
    }

    public class KbQueryIndexRebuildReply : KbBaseActionResponse, IRmQueryReply
    {
    }
}
