using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;

namespace NTDLS.Katzebase.Engine.QueryProcessing.Searchers
{
    internal class DocumentLookupResults<TData> where TData : IStringable
    {
        public List<List<TData?>> RowValues { get; private set; } = new();

        /// <summary>
        /// This is only used when we just want to return a list of document pointers and no fields.
        /// </summary>
        public List<SchemaIntersectionRowDocumentIdentifier<TData>> RowDocumentIdentifiers { get; private set; } = new();

        public void AddRange(SchemaIntersectionRowCollection<TData> rowCollection)
        {
            RowValues = rowCollection.Select(o => o.ToList()).ToList();
        }
    }
}
