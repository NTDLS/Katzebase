namespace Katzebase.Engine.Documents
{
    /// <summary>
    /// This is a simple class that contains a document page number as well as the page ID.
    /// </summary>
    public class PageDocument
    {
        public int PageNumber { get; private set; }
        public uint DocumentId { get; set; }

        public PageDocument(int pageNumber, uint documentId)
        {
            PageNumber = pageNumber;
            DocumentId = documentId;
        }
    }
}
