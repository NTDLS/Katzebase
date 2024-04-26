namespace NTDLS.Katzebase.Engine.Documents
{
    /// <summary>
    /// This is a simple class that contains a document page number as well as the page ID.
    /// </summary>
    public struct DocumentPointer
    {
        public int PageNumber { get; private set; }
        public uint DocumentId { get; set; }
        public readonly string Key => $"{PageNumber}:{DocumentId}";

        public DocumentPointer(int pageNumber, uint documentId)
        {
            PageNumber = pageNumber;
            DocumentId = documentId;
        }

        public static DocumentPointer Parse(string key)
        {
            var parts = key.Split(':');
            return new DocumentPointer(int.Parse(parts[0]), uint.Parse(parts[1]));
        }

        public class DocumentPageEqualityComparerNullable : IEqualityComparer<DocumentPointer?>
        {
            public bool Equals(DocumentPointer? x, DocumentPointer? y)
                => x?.PageNumber == y?.PageNumber && x?.DocumentId == y?.DocumentId;

            public int GetHashCode(DocumentPointer? obj)
                => HashCode.Combine(obj?.PageNumber, obj?.DocumentId);
        }

        public class DocumentPageEqualityComparer : IEqualityComparer<DocumentPointer>
        {
            public bool Equals(DocumentPointer x, DocumentPointer y)
                => x.PageNumber == y.PageNumber && x.DocumentId == y.DocumentId;

            public int GetHashCode(DocumentPointer obj)
                => obj.GetHashCode();
        }

        public int GetHashCode(DocumentPointer? obj)
            => HashCode.Combine(obj?.PageNumber, obj?.DocumentId);

        public int GetHashCode(DocumentPointer obj)
            => HashCode.Combine(obj.PageNumber, obj.DocumentId);
    }
}
