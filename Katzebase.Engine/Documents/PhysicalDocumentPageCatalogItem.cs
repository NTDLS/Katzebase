namespace Katzebase.Engine.Documents
{
    /// <summary>
    /// This is the collection item that is physically written to disk via PhysicalDocumentPageCatalog. It contains the
    ///     page number as the number of documents sotred in that page.
    /// </summary>
    [Serializable]
    public class PhysicalDocumentPageCatalogItem
    {
        public int PageNumber { get; set; }
        public int DocumentCount { get; set; }

        public PhysicalDocumentPageCatalogItem(int pageNumber)
        {
            PageNumber = pageNumber;
        }
    }
}
