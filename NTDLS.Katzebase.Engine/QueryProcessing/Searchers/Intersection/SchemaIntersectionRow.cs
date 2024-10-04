using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.PersistentTypes.Document;

namespace NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection
{
    internal class SchemaIntersectionRow
    {
        public KbInsensitiveDictionary<DocumentPointer> DocumentPointers { get; private set; } = new();

        /// <summary>
        /// A dictionary that contains the elements from each row that comprises this row.
        /// </summary>
        public KbInsensitiveDictionary<KbInsensitiveDictionary<string?>> SchemaElements { get; private set; } = new();

        /// <summary>
        /// Keeps track of the schemas that this row is comprised of, contains the schema prefixes.
        /// Another way to put it: this is the schemas that have been matched for this row.
        /// </summary>
        public HashSet<string> MatchedSchemas { get; set; } = new();

        public SchemaIntersectionRow()
        {
        }

        public SchemaIntersectionRow Clone()
        {
            return new SchemaIntersectionRow()
            {
                MatchedSchemas = new HashSet<string>(MatchedSchemas),
                DocumentPointers = DocumentPointers.Clone(),
                SchemaElements = SchemaElements.Clone(),
            };
        }
    }
}
