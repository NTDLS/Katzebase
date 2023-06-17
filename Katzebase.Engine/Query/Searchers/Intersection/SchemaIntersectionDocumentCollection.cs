namespace Katzebase.Engine.Query.Searchers.Intersection
{
    internal class SchemaIntersectionDocumentCollection
    {
        public Dictionary<string, DocumentPointerMatch> MatchedDocumentPointsPerSchema = new();
        public List<SchemaIntersectionDocumentItem> Documents { get; set; } = new();
        public int DistinctSchemaCount => Documents.Select(o => o.SchemaAlias).Distinct().Count();
    }
}
