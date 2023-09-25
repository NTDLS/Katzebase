namespace Katzebase.Engine.Documents
{
    /// <summary>
    /// This is a simple class that contains a document page number as well as the page ID.
    /// </summary>
    public class DocumentPointer
    {
        public int PageNumber { get; private set; }
        public uint DocumentId { get; set; }
        public string Key => $"{PageNumber}:{DocumentId}";

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

        public class DocumentPageEqualityComparer : IEqualityComparer<DocumentPointer>
        {
            public bool Equals(DocumentPointer? x, DocumentPointer? y)
            {
                // Compare both the PageNumber and DocumentId properties
                return x?.PageNumber == y?.PageNumber && x?.DocumentId == y?.DocumentId;
            }

            public int GetHashCode(DocumentPointer obj)
            {
                // Generate a hash code based on the PageNumber and DocumentId properties
                return HashCode.Combine(obj.PageNumber, obj.DocumentId);
            }
        }
    }
}
