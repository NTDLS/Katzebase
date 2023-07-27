using Katzebase.Engine.Documents;
using Katzebase.PublicLibrary.Exceptions;

namespace Katzebase.Engine.Query.Searchers.Intersection
{
    internal class SchemaIntersectionRow
    {
        public Dictionary<string, DocumentPointer> SchemaDocumentPointers = new();

        public List<string?> Values { get; set; } = new();

        /// <summary>
        /// The schemas that were used to make up this row.
        /// </summary>
        public HashSet<string> SchemaKeys { get; set; } = new();

        //TODO: Can probably combine these:
        public Dictionary<string, string?> AuxiliaryFields { get; set; } = new();
        //public Dictionary<string, string?> AuxiliaryFields { get; set; } = new();

        public void InsertValue(string fieldNameForException, int ordinal, string? value)
        {
            if (Values.Count <= ordinal)
            {
                int difference = (ordinal + 1) - Values.Count;
                if (difference > 0)
                {
                    Values.AddRange(new string[difference]);
                }
            }
            if (Values[ordinal] != null)
            {
                throw new KbEngineException($"Ambigious field [{fieldNameForException}].");
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

            newRow.AuxiliaryFields = AuxiliaryFields.ToDictionary(entry => entry.Key, entry => entry.Value);
            newRow.SchemaDocumentPointers = SchemaDocumentPointers.ToDictionary(entry => entry.Key, entry => entry.Value);

            return newRow;
        }
    }
}