namespace Katzebase.Engine.Documents
{
    [Serializable]
    public class PersistDocumentPageCatalogItem
    {
        public int PageNumber { get; set; }
        public HashSet<Guid> DocumentIDs { get; set; } = new();

        public PersistDocumentPageCatalogItem(int pageNumber)
        {
            PageNumber = pageNumber;
        }
    }
}
