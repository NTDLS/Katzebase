namespace Katzebase.Engine.Query.Searchers.MultiSchema.Intersection
{
    public class MSQSchemaIntersection
    {
        public List<MSQSchemaIntersectionItem> Collection { get; set; } = new();

        public Dictionary<string, HashSet<Guid>> SchemaRIDs { get; set; } = new();

        public void Add(MSQSchemaIntersectionItem result)
        {
            Collection.Add(result);
        }

        public void AddRange(List<MSQSchemaIntersectionItem> result)
        {
            Collection.AddRange(result);
        }

        public void AddRange(MSQSchemaIntersection result)
        {
            Collection.AddRange(result.Collection);
        }
    }
}
