using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;

namespace NTDLS.Katzebase.Engine.QueryProcessing.Searchers
{
    public class DocumentPageEqualityComparer : IEqualityComparer<SchemaIntersectionRowDocumentIdentifier>
    {
        public bool Equals(SchemaIntersectionRowDocumentIdentifier? x, SchemaIntersectionRowDocumentIdentifier? y)
            => x?.DocumentPointer.PageNumber == y?.DocumentPointer.PageNumber && x?.DocumentPointer.DocumentId == y?.DocumentPointer.DocumentId;

        public int GetHashCode(SchemaIntersectionRowDocumentIdentifier obj)
            => obj.GetHashCode();
    }
}
