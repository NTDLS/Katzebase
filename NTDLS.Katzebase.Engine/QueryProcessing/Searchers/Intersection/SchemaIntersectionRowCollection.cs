namespace NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection
{
    internal class SchemaIntersectionRowCollection : List<SchemaIntersectionRow>
    {
        public SchemaIntersectionRowCollection Clone()
        {
            var clone = new SchemaIntersectionRowCollection();

            foreach (var item in this)
            {
                clone.Add(item.Clone());
            }

            return clone;
        }
    }
}
