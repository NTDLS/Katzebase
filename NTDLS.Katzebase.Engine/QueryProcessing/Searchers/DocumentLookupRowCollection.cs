using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;

namespace NTDLS.Katzebase.Engine.QueryProcessing.Searchers
{
    internal class DocumentLookupRowCollection
    {
        public List<MaterializedRow> Rows { get; private set; } = new();

        /// <summary>
        /// This is only used when we just want to return a list of document pointers and no fields.
        /// </summary>
        public List<SchemaIntersectionRowDocumentIdentifier> DocumentIdentifiers { get; private set; } = new();

        public DocumentLookupRowCollection(List<MaterializedRow> rows, List<SchemaIntersectionRowDocumentIdentifier> documentIdentifiers)
        {
            Rows = rows;
            DocumentIdentifiers = documentIdentifiers;
        }
    }
}
