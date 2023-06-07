namespace Katzebase.Engine.Documents
{
    /// <summary>
    /// We may kill this class in favor for a better one that has a collection of dictonaries.
    /// </summary>
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
