using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Documents;

namespace NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection
{
    public class SchemaIntersectionRowDocumentIdentifier<TData> where TData : IStringable
    {
        public DocumentPointer DocumentPointer { get; set; }
        public KbInsensitiveDictionary<TData?> AuxiliaryFields { get; set; }

        public SchemaIntersectionRowDocumentIdentifier(DocumentPointer documentPointer, KbInsensitiveDictionary<TData?> auxiliaryFields)
        {
            DocumentPointer = documentPointer;
            AuxiliaryFields = auxiliaryFields;
        }
    }
}
