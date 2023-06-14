namespace Katzebase.Engine.Documents
{
    public class PageDocument
    {
        public int PageNumber { get; private set; }
        public Guid Id { get; set; }

        public PageDocument(Guid id, int pageNumber)
        {
            Id = id;
            PageNumber = pageNumber;
        }

        public PageDocument()
        {
            
        }
    }
}
