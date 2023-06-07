namespace Katzebase.Engine.Documents.Threading.SingleSchemaQuery
{
    public class DocumentLookupResult
    {
        public Guid RID { get; set; }
        public List<string> Values { get; set; } = new List<string>();

        public DocumentLookupResult(Guid rid)
        {
            RID = rid;
        }
    }
}
