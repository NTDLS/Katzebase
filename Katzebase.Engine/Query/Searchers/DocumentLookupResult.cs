using Katzebase.Engine.Documents;

namespace Katzebase.Engine.Query.Searchers
{
    public class DocumentLookupResult
    {
        public Dictionary<string, DocumentPointer> SchemaDocumentPointers = new();
        public List<string> Values { get; set; } = new();

        public Dictionary<string, string> ConditionFields = new();

        public DocumentLookupResult(List<string> values)
        {
            Values.AddRange(values);
        }
    }
}
