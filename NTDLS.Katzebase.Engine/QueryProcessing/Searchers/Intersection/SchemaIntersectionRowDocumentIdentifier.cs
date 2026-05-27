using NTDLS.Katzebase.Api.Types;

namespace NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection
{
    internal class SchemaIntersectionRowDocumentIdentifier
        (uint documentId, KbInsensitiveDictionary<KbInsensitiveDictionary<string?>> schemaElements)
    {
        public uint DocumentId { get; set; } = documentId;
        public KbInsensitiveDictionary<KbInsensitiveDictionary<string?>> SchemaElements { get; set; } = schemaElements;
    }
}
