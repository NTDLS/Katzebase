using NTDLS.Katzebase.Api.Models;
using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.ReliableMessaging;

namespace NTDLS.Katzebase.Api.Payloads
{
    /// <summary>
    /// Gets a single document by its id.
    /// </summary>
    public class KbQueryDocumentGet : IRmQuery<KbQueryDocumentGetReply>
    {
        public Guid ConnectionId { get; set; }
        public string Schema { get; set; }
        public uint DocumentId { get; set; }

        public KbQueryDocumentGet(Guid connectionId, string schema, uint documentId)
        {
            ConnectionId = connectionId;
            Schema = schema;
            DocumentId = documentId;
        }
    }

    public class KbQueryDocumentGetReply : KbBaseActionResponse, IRmQueryReply
    {
        /// <summary>
        /// The document, or null if no document with the requested id exists.
        /// </summary>
        public KbDocument? Document { get; set; }
    }
}
