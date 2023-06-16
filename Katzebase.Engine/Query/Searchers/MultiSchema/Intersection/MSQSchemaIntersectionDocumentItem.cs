namespace Katzebase.Engine.Query.Searchers.MultiSchema.Intersection
{
    public class MSQSchemaIntersectionDocumentItem
    {
        public string SchemaAlias { get; set; }
        public uint DocumentId { get; set; }
        public Dictionary<string, string> Values { get; set; } = new();

        public MSQSchemaIntersectionDocumentItem(string schemaAlias, uint documentId)
        {
            SchemaAlias = schemaAlias;
            DocumentId = documentId;
        }
    }
}
