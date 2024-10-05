using static NTDLS.Katzebase.Engine.QueryProcessing.Searchers.StaticSearcherMethods;

namespace NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection
{
    internal class MaterializedRowCollection
    {
        public List<MaterializedRow> Rows { get; private set; } = new();

        /// <summary>
        /// This is only used when we just want to return a list of document pointers and no fields.
        /// </summary>
        public List<SchemaIntersectionRowDocumentIdentifier> DocumentIdentifiers { get; private set; } = new();
    }
}
