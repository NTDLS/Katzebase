namespace Katzebase.Engine.Documents.Threading.MultiSchemaQuery.SchemaMapping
{
    public class MSQSchemaIntersection
    {
        public List<MSQSchemaMapResult> Collection { get; set; } = new();

        public Dictionary<string, HashSet<Guid>> SchemaRIDs { get; set; } = new();

        public void Add(MSQSchemaMapResult result)
        {
            Collection.Add(result);
        }

        public void AddRange(List<MSQSchemaMapResult> result)
        {
            Collection.AddRange(result);
        }

        public void AddRange(MSQSchemaIntersection result)
        {
            Collection.AddRange(result.Collection);
        }
    }
}
