using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Documents;
using fs;
namespace NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection
{
    public class SchemaIntersectionRowDocumentIdentifier
    {
        public DocumentPointer DocumentPointer { get; set; }
        public KbInsensitiveDictionary<fstring, fstring?> AuxiliaryFields { get; set; }

        public SchemaIntersectionRowDocumentIdentifier(DocumentPointer documentPointer, KbInsensitiveDictionary<fstring, fstring?> auxiliaryFields)
        {
            DocumentPointer = documentPointer;
            AuxiliaryFields = auxiliaryFields;
        }
    }
}
