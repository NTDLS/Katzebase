using Katzebase.Engine.Documents;

namespace Katzebase.Engine.Query.Searchers.MultiSchema.Intersection
{
    internal class MSQSchemaIntersectionDocumentCollection
    {
        public Dictionary<string, PageDocumentMatch> MatchedDocumentIDsPerSchema = new ();
        public List<MSQSchemaIntersectionDocumentItem> Documents { get; set; } = new();
        public int DistinctSchemaCount => Documents.Select(o => o.SchemaAlias).Distinct().Count();
    }
}
