using NTDLS.Katzebase.Api.Types;
using NTDLS.Katzebase.PersistentTypes.Document;

namespace NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection
{
    internal class SchemaIntersectionRowDocumentIdentifier
    {
        public DocumentPointer DocumentPointer { get; set; }
        public KbInsensitiveDictionary<KbInsensitiveDictionary<string?>> SchemaElements { get; set; }

        public SchemaIntersectionRowDocumentIdentifier(DocumentPointer documentPointer, KbInsensitiveDictionary<KbInsensitiveDictionary<string?>> schemaElements)
        {
            DocumentPointer = documentPointer;
            SchemaElements = schemaElements;
        }
    }
}
