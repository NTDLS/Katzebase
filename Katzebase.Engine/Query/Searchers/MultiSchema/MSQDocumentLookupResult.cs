using Katzebase.Engine.Documents;

namespace Katzebase.Engine.Query.Searchers.MultiSchema
{
    public class MSQDocumentLookupResult
    {
        public PageDocument PageDocument { get; set; }
        public List<string> Values { get; set; } = new();
        public Dictionary<string, string> ConditionFields = new();

        public void InsertValue(int ordinal, string value)
        {
            //We do not accumulate values in the same order that they were requested by the query, we need to put them in the right place.
            Values[ordinal] = value;
        }

        public MSQDocumentLookupResult(PageDocument pageDocument, int fieldCount)
        {
            PageDocument = pageDocument;
            Values.AddRange(new string[fieldCount]);
        }
    }
}
