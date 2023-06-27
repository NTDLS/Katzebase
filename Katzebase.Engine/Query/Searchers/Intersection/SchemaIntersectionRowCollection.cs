namespace Katzebase.Engine.Query.Searchers.Intersection
{
    internal class SchemaIntersectionRowCollection
    {
        public List<SchemaIntersectionRow> Rows { get; set; } = new();

        public void Add(SchemaIntersectionRow row)
        {
            Rows.Add(row);
        }
    }
}
