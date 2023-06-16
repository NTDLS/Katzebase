namespace Katzebase.Engine.Query.Searchers.SingleSchema
{
    public class SSQDocumentLookupResult
    {
        public uint DocumentId { get; set; }
        public List<string> Values { get; set; } = new List<string>();

        public SSQDocumentLookupResult(uint documentId)
        {
            DocumentId = documentId;
        }
    }
}
