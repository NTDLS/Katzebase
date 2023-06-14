using System.Collections.Generic;

namespace Katzebase.Engine.Documents
{
    [Serializable]
    public class PhysicalDocumentPageCatalog
    {
        public List<PhysicalDocumentPageCatalogItem> Collection { get; private set; } = new();

        public int NextPageNumber() => Collection.Count;

        public IEnumerable<PageDocument> ConsolidatedPageDocuments()
        {
            return Collection.SelectMany(o => o.DocumentIDs.Select(h => new PageDocument(h, o.PageNumber)));
        }

        public IEnumerable<PageDocument> Where(Guid documentId)
        {
            return Collection.SelectMany(o => o.DocumentIDs.Where(g => g == documentId).Select(h => new PageDocument(h, o.PageNumber)));
        }

        public IEnumerable<PageDocument> Where(HashSet<Guid> documentIds)
        {
            return Collection.SelectMany(o => o.DocumentIDs.Where(g => documentIds.Contains(g)).Select(h => new PageDocument(h, o.PageNumber)));
        }

        public PhysicalDocumentPageCatalogItem? GetDocumentPageByDocumentId(Guid documentId)
        {
            foreach (var documentPage in Collection)
            {
                if (documentPage.DocumentIDs.Contains(documentId))
                {
                    return documentPage;
                }
            }

            return null;
        }

        public PhysicalDocumentPageCatalogItem? GetPageWithRoomForNewDocument(int pageSize)
        {
            //TODO: Make the page size configurable.
            return Collection.Where(o => o.DocumentIDs.Count < pageSize).FirstOrDefault();
        }
    }
}
