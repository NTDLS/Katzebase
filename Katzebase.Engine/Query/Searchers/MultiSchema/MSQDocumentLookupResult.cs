using Katzebase.Engine.Documents;

namespace Katzebase.Engine.Query.Searchers.MultiSchema
{
    public class MSQDocumentLookupResult
    {
        public DocumentPointer DocumentPointer { get; set; }
        public List<string> Values { get; set; } = new();
        public Dictionary<string, string> ConditionFields = new();

        public void InsertValue(int ordinal, string value)
        {
            //We do not accumulate values in the same order that they were requested by the query, we need to put them in the right place.
            Values[ordinal] = value;
        }

        public MSQDocumentLookupResult(DocumentPointer documentPointer, int fieldCount)
        {
            DocumentPointer = documentPointer;
            Values.AddRange(new string[fieldCount]);
        }
    }
}
