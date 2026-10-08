using NTDLS.Katzebase.Api.Models;
using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.ReliableMessaging;

namespace NTDLS.Katzebase.Api.Payloads
{
    /// <summary>
    /// Replaces the entire content of an existing document, keeping its id. Indexes are updated accordingly.
    /// </summary>
    public class KbQueryDocumentReplace : IRmQuery<KbQueryDocumentReplaceReply>
    {
        public Guid ConnectionId { get; set; }
        public string Schema { get; set; }
        public uint DocumentId { get; set; }
        public KbDocument Document { get; set; }

        public KbQueryDocumentReplace(Guid connectionId, string schema, uint documentId, KbDocument document)
        {
            ConnectionId = connectionId;
            Schema = schema;
            DocumentId = documentId;
            Document = document;
        }
    }

    public class KbQueryDocumentReplaceReply : KbBaseActionResponse, IRmQueryReply
    {
    }
}
