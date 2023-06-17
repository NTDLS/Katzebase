using Katzebase.Engine.Documents;

namespace Katzebase.Engine.Query.Searchers
{
    public class DocumentLookupResults
    {
        public List<DocumentLookupResult> Collection { get; set; } = new();

        /// <summary>
        /// This is only used when we just want to return a list of document pointers and no fields.
        /// </summary>
        public List<DocumentPointer> DocumentPointers { get; set; } = new();

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
