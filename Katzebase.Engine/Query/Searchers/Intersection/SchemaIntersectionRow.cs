using Katzebase.Engine.Documents;

namespace Katzebase.Engine.Query.Searchers.Intersection
{
    internal class SchemaIntersectionRow
    {
        public Dictionary<string, DocumentPointer> SchemaDocumentPointers = new();

        public List<string> Values { get; private set; } = new List<string>();

        /// <summary>
        /// The schemas that were used to make up this row.
        /// </summary>
        public HashSet<string> SchemaKeys { get; set; } = new();

        //TODO: Can probably combine these:
        public Dictionary<string, string> ConditionFields { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> MethodFields { get; set; } = new Dictionary<string, string>();

        public void InsertValue(int ordinal, string value)
        {
            if (Values.Count <= ordinal)
            {
                int difference = (ordinal + 1) - Values.Count;
                if (difference > 0)
                {
                    Values.AddRange(new string[difference]);
                }
            }
            Values[ordinal] = value;
        }

        public void AddSchemaDocumentPointer(string schemaPrefix, DocumentPointer documentPointer)
        {
            SchemaDocumentPointers.Add(schemaPrefix, documentPointer);
        }

        public SchemaIntersectionRow Clone()
        {
            var newRow = new SchemaIntersectionRow
            {
                SchemaKeys = new HashSet<string>(SchemaKeys)
            };

            newRow.Values.AddRange(Values);

            newRow.ConditionFields = ConditionFields.ToDictionary(entry => entry.Key, entry => entry.Value);
            newRow.MethodFields = MethodFields.ToDictionary(entry => entry.Key, entry => entry.Value);
            newRow.SchemaDocumentPointers = SchemaDocumentPointers.ToDictionary(entry => entry.Key, entry => entry.Value);

            return newRow;
        }
    }
}