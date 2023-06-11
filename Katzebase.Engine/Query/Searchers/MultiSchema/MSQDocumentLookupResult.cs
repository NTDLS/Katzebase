namespace Katzebase.Engine.Query.Searchers.MultiSchema
{
    public class MSQDocumentLookupResult
    {
        public Guid RID { get; set; }
        public List<string> Values { get; set; } = new List<string>();

        public MSQDocumentLookupResult(Guid rid)
        {
            RID = rid;
        }
    }
}
