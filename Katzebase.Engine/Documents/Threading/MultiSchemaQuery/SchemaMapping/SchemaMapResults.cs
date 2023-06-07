namespace Katzebase.Engine.Documents.Threading.MultiSchemaQuery.SchemaMapping
{
    public class SchemaMapResults
    {
        public List<SchemaMapResult> Collection { get; set; } = new();

        public Dictionary<string, HashSet<Guid>> SchemaRIDs { get; set; } = new();

        public void Add(SchemaMapResult result)
        {
            Collection.Add(result);
        }

        public void AddRange(List<SchemaMapResult> result)
        {
            Collection.AddRange(result);
        }

        public void AddRange(SchemaMapResults result)
        {
            Collection.AddRange(result.Collection);
        }
    }
}
