namespace Katzebase.Engine.Query.Searchers.Intersection
{
    internal class SchemaIntersectionRowCollection
    {
        public List<SchemaIntersectionRow> Collection { get; set; } = new();

        public void Add(SchemaIntersectionRow row)
        {
            Collection.Add(row);
        }

        public void AddRange(SchemaIntersectionRowCollection rows)
        {
            Collection.AddRange(rows.Collection);
        }
    }
}
