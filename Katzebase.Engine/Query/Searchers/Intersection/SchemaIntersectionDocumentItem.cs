namespace Katzebase.Engine.Query.Searchers.Intersection
{
    public class SchemaIntersectionDocumentItem
    {
        public string SchemaAlias { get; set; }
        public uint DocumentId { get; set; }
        public Dictionary<string, string> Values { get; set; } = new();

        public SchemaIntersectionDocumentItem(string schemaAlias, uint documentId)
        {
            SchemaAlias = schemaAlias;
            DocumentId = documentId;
        }
    }
}
