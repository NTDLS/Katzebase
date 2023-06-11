namespace Katzebase.Engine.Query.Searchers.MultiSchema.Intersection
{
    public class MSQSchemaIntersectionDocumentItem
    {
        public string SchemaAlias { get; set; }
        public Guid DocumentIDs { get; set; }
        public Dictionary<string, string> Values { get; set; } = new();

        public MSQSchemaIntersectionDocumentItem(string schemaAlias, Guid documentId)
        {
            SchemaAlias = schemaAlias;
            DocumentIDs = documentId;
        }
    }
}
