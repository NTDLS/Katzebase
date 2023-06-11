namespace Katzebase.Engine.Query.Searchers.MultiSchema.Intersection
{
    public class MSQSchemaIntersectionDocumentCollection
    {
        public Dictionary<string, HashSet<Guid>> MatchedDocumentIDsPerSchema = new Dictionary<string, HashSet<Guid>>();

        public List<MSQSchemaIntersectionDocumentItem> Documents { get; set; } = new();
        public int DistinctSchemaCount => Documents.Select(o => o.SchemaAlias).Distinct().Count();
    }
}
