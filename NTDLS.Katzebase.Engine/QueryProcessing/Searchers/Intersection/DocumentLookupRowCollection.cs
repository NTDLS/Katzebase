namespace NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection
{
    internal class DocumentLookupRowCollection(List<MaterializedRow> rows, List<SchemaIntersectionRowDocumentIdentifier> documentIdentifiers)
    {
        public List<MaterializedRow> Rows { get; private set; } = rows;

        /// <summary>
        /// This is only used when we just want to return a list of document pointers and no fields.
        /// </summary>
        public List<SchemaIntersectionRowDocumentIdentifier> DocumentIdentifiers { get; private set; } = documentIdentifiers;
    }
}
