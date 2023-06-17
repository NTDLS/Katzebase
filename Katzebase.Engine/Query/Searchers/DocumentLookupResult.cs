using Katzebase.Engine.Documents;

namespace Katzebase.Engine.Query.Searchers
{
    public class DocumentLookupResult
    {
        public DocumentPointer DocumentPointer { get; set; }
        public List<string> Values { get; set; } = new();
        public Dictionary<string, string> ConditionFields = new();

        public void InsertValue(int ordinal, string value)
        {
            //We do not accumulate values in the same order that they were requested by the query, we need to put them in the right place.
            Values[ordinal] = value;
        }

        public DocumentLookupResult(DocumentPointer documentPointer, int fieldCount)
        {
            DocumentPointer = documentPointer;
            Values.AddRange(new string[fieldCount]);
        }

        public void Resize(int newCount)
        {
            int difference = newCount - Values.Count;
            if (difference > 0)
            {
                Values.AddRange(new string[difference]);
            }
        }

    }
}
