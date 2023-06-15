namespace Katzebase.Engine.Documents
{
    /// <summary>
    /// This is the master document page catalog, it is physically written to disk and
    /// contains one entry per page, each of which contain the associated socumentIDs.
    /// </summary>
    [Serializable]
    public class PhysicalDocumentPageCatalog
    {
        public List<PhysicalDocumentPageMap> PageMappings { get; private set; } = new();

        public int NextPageNumber() => PageMappings.Count;

        public int TotalPageCount()
        {
            return PageMappings.SelectMany(o => o.DocumentIDs).Count();
        }


        public IEnumerable<PageDocument> ConsolidatedPageDocuments()
        {
            return PageMappings.SelectMany(o => o.DocumentIDs.Select(h => new PageDocument(h, o.PageNumber)));
        }

        public IEnumerable<PageDocument> FindPageDocument(Guid documentId)
        {
            return PageMappings.SelectMany(o => o.DocumentIDs.Where(g => g == documentId).Select(h => new PageDocument(h, o.PageNumber)));
        }

        public IEnumerable<PageDocument> FindPageDocuments(HashSet<Guid> documentIds)
        {
            return PageMappings.SelectMany(o => o.DocumentIDs.Where(g => documentIds.Contains(g)).Select(h => new PageDocument(h, o.PageNumber)));
        }

        public PhysicalDocumentPageMap? GetDocumentPageMap(Guid documentId)
        {
            foreach (var map in PageMappings)
            {
                if (map.DocumentIDs.Contains(documentId))
                {
                    return map;
                }
            }

            return null;
        }

        public PhysicalDocumentPageMap? GetPageWithRoomForNewDocument(int pageSize)
        {
            //TODO: Make the page size configurable.
            return PageMappings.Where(o => o.DocumentIDs.Count < pageSize).FirstOrDefault();
        }
    }
}
