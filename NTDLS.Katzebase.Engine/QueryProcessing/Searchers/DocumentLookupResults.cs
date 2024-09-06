using NTDLS.Katzebase.Engine.Documents;
using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;

namespace NTDLS.Katzebase.Engine.QueryProcessing.Searchers
{
    internal class DocumentLookupResults
    {
        public List<DocumentLookupResult> Collection { get; private set; } = new();

        /// <summary>
        /// This is only used when we just want to return a list of document pointers and no fields.
        /// </summary>
        public List<DocumentPointer> DocumentPointers { get; private set; } = new();

        public DocumentLookupResults()
        {
        }

        public void Add(DocumentLookupResult result)
        {
            Collection.Add(result);
        }

        public void AddRange(List<SchemaIntersectionRow> rows)
        {
            Collection.AddRange(rows.Select(o => new DocumentLookupResult(o.Values)));
        }

        public void AddRange(SchemaIntersectionRowCollection rowCollection)
        {
            Collection.AddRange(rowCollection.Collection.Select(o => new DocumentLookupResult(o.Values)));
        }
    }
}
