using Katzebase.Engine.Documents;

namespace Katzebase.Engine.Query.Searchers
{
    public class DocumentLookupResult
    {
        public Dictionary<string, DocumentPointer> SchemaDocumentPointers = new();
        public List<string> Values { get; set; } = new();

        public Dictionary<string, string> ConditionFields = new();

        public void InsertValue(int ordinal, string value)
        {
            //We do not accumulate values in the same order that they were requested by the query, we need to put them in the right place.
            if (Values.Count <= ordinal)
            {
                Resize(ordinal + 1);
            }
            Values[ordinal] = value;
        }

        public DocumentLookupResult(int fieldCount)
        {
            Values.AddRange(new string[fieldCount]);
        }

        public void AddSchemaDocumentPointer(string schemaPrefix, DocumentPointer documentPointer)
        {
            SchemaDocumentPointers.Add(schemaPrefix, documentPointer);
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
