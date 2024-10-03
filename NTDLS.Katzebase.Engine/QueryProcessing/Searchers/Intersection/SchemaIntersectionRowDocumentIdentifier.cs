using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Documents;
using NTDLS.Katzebase.Parsers.Interfaces;
namespace NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection
{
    public class SchemaIntersectionRowDocumentIdentifier<TData> where TData : IStringable
    {
        public DocumentPointer<TData> DocumentPointer { get; set; }
        public KbInsensitiveDictionary<TData?> AuxiliaryFields { get; set; }

        public SchemaIntersectionRowDocumentIdentifier(DocumentPointer<TData> documentPointer, KbInsensitiveDictionary<TData?> auxiliaryFields)
        {
            DocumentPointer = documentPointer;
            AuxiliaryFields = auxiliaryFields;
        }
    }
}
