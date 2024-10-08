namespace NTDLS.Katzebase.PersistentTypes.Document
{
    /// <summary>
    /// This is a simple class that contains a document page number as well as the page ID.
    /// </summary>
    public class DocumentPointer(int pageNumber, uint documentId)
    {
        public int PageNumber { get; private set; } = pageNumber;
        public uint DocumentId { get; set; } = documentId;
        public string Key => $"{PageNumber}:{DocumentId}";

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

        public override string ToString()
            => Key;

        public override int GetHashCode()
            => HashCode.Combine(PageNumber, DocumentId);
    }
}
