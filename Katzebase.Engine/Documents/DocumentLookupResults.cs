namespace Katzebase.Engine.Documents
{
    internal class DocumentLookupResults
    {
        public List<DocumentLookupResult> Collection { get; set; } = new();

        public void Add(DocumentLookupResult result)
        {
            Collection.Add(result);
        }

        public void AddRange(List<DocumentLookupResult> result)
        {
            Collection.AddRange(result);
        }

        public void AddRange(DocumentLookupResults result)
        {
            Collection.AddRange(result.Collection);
        }
    }
}
