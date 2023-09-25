using Katzebase.Engine.Documents;
using Katzebase.Engine.Query.Searchers.Intersection;

namespace Katzebase.Engine.Query.Searchers
{
    internal class DocumentLookupResults
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
