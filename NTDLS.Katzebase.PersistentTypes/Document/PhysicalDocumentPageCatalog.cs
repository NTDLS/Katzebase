using ProtoBuf;

namespace NTDLS.Katzebase.PersistentTypes.Document
{
    /// <summary>
    /// This is the master document page catalog, it is physically written to disk and
    /// contains one entry per page, each of which contain the associated documentIDs.
    /// </summary>
    [ProtoContract]
    [Serializable]
    public class PhysicalDocumentPageCatalog
    {
        public PhysicalDocumentPageCatalog() { }

        [ProtoIgnore]
        private List<PhysicalDocumentPageCatalogItem>? _catalog;

        [ProtoMember(1)]
        public List<PhysicalDocumentPageCatalogItem> Catalog
        {
            get
            {
                _catalog ??= new List<PhysicalDocumentPageCatalogItem>();
                return _catalog;
            }
            set
            {
                _catalog = value;
            }
        }

        [ProtoMember(2)]
        public uint NextDocumentId { get; set; } = 1;

        public uint ConsumeNextDocumentId()
        {
            return NextDocumentId++;
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

        public PhysicalDocumentPageCatalogItem? GetPageWithRoomForNewDocument(uint pageSize)
        {
            //TODO: Make the page size configurable.
            return Catalog.FirstOrDefault(o => o.DocumentCount < pageSize);
        }
    }
}
