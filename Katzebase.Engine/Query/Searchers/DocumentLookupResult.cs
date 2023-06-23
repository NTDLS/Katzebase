namespace Katzebase.Engine.Query.Searchers
{
    public class DocumentLookupResult
    {
        public List<string> Values { get; set; } = new();

        public DocumentLookupResult(List<string> values)
        {
            Values.AddRange(values);
        }
    }
}
