namespace Katzebase.Engine.Documents
{
    [Serializable]
    public class PersistDocumentPage
    {
        public int PageNumber { get; private set; }

        public Dictionary<Guid, PersistDocument> Documents { get; set; } = new();

        public PersistDocumentPage(int pageNumber)
        {
            PageNumber = pageNumber;
        }
    }
}
