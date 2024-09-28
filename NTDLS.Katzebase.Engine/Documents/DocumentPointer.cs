using NTDLS.Katzebase.Engine.QueryProcessing.Searchers.Intersection;

namespace NTDLS.Katzebase.Engine.Documents
{
    /// <summary>
    /// This is a simple class that contains a document page number as well as the page ID.
    /// </summary>
    public class DocumentPointer<TData> where TData : IStringable
    {
        public int PageNumber { get; private set; }
        public uint DocumentId { get; set; }
        public string Key => $"{PageNumber}:{DocumentId}";

        public DocumentPointer(int pageNumber, uint documentId)
        {
            PageNumber = pageNumber;
            DocumentId = documentId;
        }

        public static DocumentPointer<TData> Parse(string key)
        {
            var parts = key.Split(':');
            return new DocumentPointer<TData>(int.Parse(parts[0]), uint.Parse(parts[1]));
        }

        public class DocumentPageEqualityComparerNullable : IEqualityComparer<DocumentPointer<TData>?>
        {
            public bool Equals(DocumentPointer<TData>? x, DocumentPointer<TData>? y)
                => x?.PageNumber == y?.PageNumber && x?.DocumentId == y?.DocumentId;

            public int GetHashCode(DocumentPointer<TData>? obj)
                => HashCode.Combine(obj?.PageNumber, obj?.DocumentId);
        }

        public class DocumentPageEqualityComparer : IEqualityComparer<SchemaIntersectionRowDocumentIdentifier<TData>>
        {
            public bool Equals(SchemaIntersectionRowDocumentIdentifier<TData>? x, SchemaIntersectionRowDocumentIdentifier<TData>? y)
                => x?.DocumentPointer.PageNumber == y?.DocumentPointer.PageNumber && x?.DocumentPointer.DocumentId == y?.DocumentPointer.DocumentId;

            public int GetHashCode(SchemaIntersectionRowDocumentIdentifier<TData> obj)
                => obj.GetHashCode();
        }

        public override string ToString()
            => Key;

        public int GetHashCode(DocumentPointer<TData>? obj)
            => HashCode.Combine(obj?.PageNumber, obj?.DocumentId);
    }
}
