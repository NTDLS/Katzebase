namespace Katzebase.Engine.Documents
{
    [Serializable]
    public class PhysicalDocumentPageCatalogItem
    {
        public int PageNumber { get; set; }
        public HashSet<Guid> DocumentIDs { get; set; } = new();

        public PhysicalDocumentPageCatalogItem(int pageNumber)
        {
            PageNumber = pageNumber;
        }
    }
}
