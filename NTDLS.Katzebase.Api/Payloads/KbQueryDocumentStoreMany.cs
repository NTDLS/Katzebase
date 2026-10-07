using NTDLS.Katzebase.Api.Models;
using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.ReliableMessaging;

namespace NTDLS.Katzebase.Api.Payloads
{
    /// <summary>
    /// Stores a batch of documents in a single round trip. The batch is inserted atomically: either all
    /// of the documents are stored or (on error) none of them are.
    /// </summary>
    public class KbQueryDocumentStoreMany : IRmQuery<KbQueryDocumentStoreManyReply>
    {
        public Guid ConnectionId { get; set; }
        public string Schema { get; set; }
        public List<KbDocument> Documents { get; set; }

        public KbQueryDocumentStoreMany(Guid connectionId, string schema, List<KbDocument> documents)
        {
            ConnectionId = connectionId;
            Schema = schema;
            Documents = documents;
        }
    }

    public class KbQueryDocumentStoreManyReply : KbBaseActionResponse, IRmQueryReply
    {
        /// <summary>
        /// The ids of the stored documents, in the same order as the documents in the request.
        /// </summary>
        public List<uint> DocumentIds { get; set; } = new();
    }
}
