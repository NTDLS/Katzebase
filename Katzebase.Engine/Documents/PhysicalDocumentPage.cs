namespace Katzebase.Engine.Documents
{
    [Serializable]
    public class PhysicalDocumentPage
    {
        public int PageNumber { get; private set; }

        public Dictionary<Guid, PhysicalDocument> Documents { get; set; } = new();

        public PhysicalDocumentPage(int pageNumber)
        {
            PageNumber = pageNumber;
        }
    }
}
