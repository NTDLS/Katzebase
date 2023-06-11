namespace Katzebase.Engine.Query.Searchers.MultiSchema
{
    public class MSQDocumentLookupResults
    {
        public List<MSQDocumentLookupResult> Collection { get; set; } = new();

        public void Add(MSQDocumentLookupResult result)
        {
            Collection.Add(result);
        }

        public void AddRange(List<MSQDocumentLookupResult> result)
        {
            Collection.AddRange(result);
        }

        public void AddRange(MSQDocumentLookupResults result)
        {
            Collection.AddRange(result.Collection);
        }
    }
}
