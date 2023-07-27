﻿namespace Katzebase.Engine.Documents
{
    /// <summary>
    /// This is the master document page catalog, it is physically written to disk and
    /// contains one entry per page, each of which contain the associated documentIDs.
    /// </summary>
    [Serializable]
    public class PhysicalDocumentPageCatalog
    {
        public List<PhysicalDocumentPageCatalogItem> Catalog { get; private set; } = new();

        public uint NextDocumentId { get; set; } = 0;

        public uint ConsumeNextDocumentId()
        {
            NextDocumentId++;
            return NextDocumentId;
        }

        public int NextPageNumber() => Catalog.Count;

        public int TotalDocumentCount()
        {
            return Catalog.Sum(o => o.DocumentCount);
        }

        /*
        public IEnumerable<DocumentPointer> ConsolidatedDocumentPointers()
        {
            return PageMappings.SelectMany(o => o.DocumentIDs.Select(h => new DocumentPointer(o.PageNumber, h)));
        }

        public IEnumerable<DocumentPointer> FindDocumentPointer(uint documentId)
        {
            return PageMappings.SelectMany(o => o.DocumentIDs.Where(g => g == documentId).Select(h => new DocumentPointer(o.PageNumber, h)));
        }

        public IEnumerable<DocumentPointer> FindDocumentPointers(HashSet<uint> documentIds)
        {
            return PageMappings.SelectMany(o => o.DocumentIDs.Where(g => documentIds.Contains(g)).Select(h => new DocumentPointer(o.PageNumber, h)));
        }

        public PhysicalDocumentPageCatalogItem? GetDocumentPageMap(uint documentId)
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
        */

        public PhysicalDocumentPageCatalogItem? GetPageWithRoomForNewDocument(int pageSize)
        {
            //TODO: Make the page size configurable.
            return Catalog.Where(o => o.DocumentCount < pageSize).FirstOrDefault();
        }
    }
}
