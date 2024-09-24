using fs;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Documents;

namespace NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection
{
    internal class SchemaIntersectionRow : List<fstring?>
    {
        public KbInsensitiveDictionary<DocumentPointer> SchemaDocumentPointers { get; private set; } = new();

        /// <summary>
        /// The schemas that were used to make up this row.
        /// </summary>
        public HashSet<string> SchemaKeys { get; set; } = new();

        /// <summary>
        /// Auxiliary fields are values that may be used for method calls, sorting, grouping, etc.
        ///     where the fields value may not necessarily be returned directly in the results.
        /// </summary>
        public KbInsensitiveDictionary<fstring, fstring?> AuxiliaryFields { get; private set; } = new(fstring.CompareFunc);

        public void InsertValue(string fieldNameForException, int ordinal, fstring? value)
        {
            if (Count <= ordinal)
            {
                int difference = ordinal + 1 - Count;
                if (difference > 0)
                {
                    AddRange(new fstring[difference]);
                }
            }
            //if (this[ordinal] != null)
            if (fstring.Compare(this[ordinal], fstring.SNull) != 0)
            {
                throw new KbEngineException($"Ambiguous field [{fieldNameForException}].");
            }

            this[ordinal] = value;
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

            newRow.AddRange(this);

            newRow.AuxiliaryFields = AuxiliaryFields.Clone();
            newRow.SchemaDocumentPointers = SchemaDocumentPointers.Clone();

            return newRow;
        }
    }
}
