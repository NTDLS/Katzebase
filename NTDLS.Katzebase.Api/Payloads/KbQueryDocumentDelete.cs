using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.ReliableMessaging;

namespace NTDLS.Katzebase.Api.Payloads
{
    /// <summary>
    /// Deletes documents by id, atomically. Ids that do not exist are ignored.
    /// </summary>
    public class KbQueryDocumentDelete : IRmQuery<KbQueryDocumentDeleteReply>
    {
        public Guid ConnectionId { get; set; }
        public string Schema { get; set; }
        public List<uint> DocumentIds { get; set; }

        public KbQueryDocumentDelete(Guid connectionId, string schema, List<uint> documentIds)
        {
            ConnectionId = connectionId;
            Schema = schema;
            DocumentIds = documentIds;
        }
    }

    public class KbQueryDocumentDeleteReply : KbBaseActionResponse, IRmQueryReply
    {
        /// <summary>
        /// The number of documents that existed and were deleted.
        /// </summary>
        public int DeletedCount { get; set; }
    }
}
