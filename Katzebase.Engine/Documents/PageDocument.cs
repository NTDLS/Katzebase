namespace Katzebase.Engine.Documents
{
    /// <summary>
    /// This is a simple class that contains a document page number as well as the page ID.
    /// </summary>
    public class PageDocument
    {
        public int PageNumber { get; private set; }
        public Guid Id { get; set; }

        public PageDocument(Guid id, int pageNumber)
        {
            Id = id;
            PageNumber = pageNumber;
        }
    }
}
