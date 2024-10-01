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
            get => _catalog ??= new List<PhysicalDocumentPageCatalogItem>();
            set => _catalog = value;
        }

        [ProtoMember(2)]
        public uint NextDocumentId { get; set; } = 1;

        public uint ConsumeNextDocumentId()
            => NextDocumentId++;

        public int NextPageNumber()
            => Catalog.Count;

        public int TotalDocumentCount()
            => Catalog.Sum(o => o.DocumentCount);

        public PhysicalDocumentPageCatalogItem? GetPageWithRoomForNewDocument(uint pageSize)
            => Catalog.FirstOrDefault(o => o.DocumentCount < pageSize);
    }
}
