namespace Katzebase.Engine.Documents
{
    /// <summary>
    /// This is a simple class that contains a document page number as well as the page ID.
    /// </summary>
    public class DocumentPointer
    {
        public int PageNumber { get; private set; }
        public uint DocumentId { get; set; }

        public DocumentPointer(int pageNumber, uint documentId)
        {
            PageNumber = pageNumber;
            DocumentId = documentId;
        }
    }
}
