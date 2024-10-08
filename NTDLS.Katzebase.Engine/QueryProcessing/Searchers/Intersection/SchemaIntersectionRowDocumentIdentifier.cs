using NTDLS.Katzebase.Api.Types;
using NTDLS.Katzebase.PersistentTypes.Document;

namespace NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection
{
    internal class SchemaIntersectionRowDocumentIdentifier
        (DocumentPointer documentPointer, KbInsensitiveDictionary<KbInsensitiveDictionary<string?>> schemaElements)
    {
        public DocumentPointer DocumentPointer { get; set; } = documentPointer;
        public KbInsensitiveDictionary<KbInsensitiveDictionary<string?>> SchemaElements { get; set; } = schemaElements;
    }
}
