using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.PersistentTypes.Document;

namespace NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection
{
    public class SchemaIntersectionRowDocumentIdentifier
    {
        public DocumentPointer DocumentPointer { get; set; }
        public KbInsensitiveDictionary<string?> AuxiliaryFields { get; set; }

        public SchemaIntersectionRowDocumentIdentifier(DocumentPointer documentPointer, KbInsensitiveDictionary<string?> auxiliaryFields)
        {
            DocumentPointer = documentPointer;
            AuxiliaryFields = auxiliaryFields;
        }
    }
}
