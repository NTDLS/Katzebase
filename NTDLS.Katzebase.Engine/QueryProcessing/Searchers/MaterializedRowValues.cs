using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;

namespace NTDLS.Katzebase.Engine.QueryProcessing.Searchers
{
    internal class MaterializedRowValues
    {
        public List<string> OrderBy { get; private set; } = new();
        public List<string> GroupBy { get; private set; } = new();

        /// <summary>
        /// Contains the list of field values for the grouping fields, and the need-to-be aggregated values for fields
        /// that are needed to collapse aggregation functions. The key is the concatenated values from the grouping fields.
        /// </summary>
        public Dictionary<string, GroupRowCollection> GroupRows { get; set; } = new();

        public List<List<string?>> Values { get; private set; } = new();

        /// <summary>
        /// This is only used when we just want to return a list of document pointers and no fields.
        /// </summary>
        public List<SchemaIntersectionRowDocumentIdentifier> DocumentIdentifiers { get; private set; } = new();
    }
}
