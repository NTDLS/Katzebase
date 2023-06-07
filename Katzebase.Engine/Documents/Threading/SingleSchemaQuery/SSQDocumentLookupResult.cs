namespace Katzebase.Engine.Documents.Threading.SingleSchemaQuery
{
    public class SSQDocumentLookupResult
    {
        public Guid RID { get; set; }
        public List<string> Values { get; set; } = new List<string>();

        public SSQDocumentLookupResult(Guid rid)
        {
            RID = rid;
        }
    }
}
