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

        public SchemaIntersectionRow()
        {
        }

        public SchemaIntersectionRow Clone()
        {
            return new SchemaIntersectionRow()
            {
                DocumentPointers = DocumentPointers.Clone(),
                SchemaElements = SchemaElements.Clone(),
            };
        }
    }
}
