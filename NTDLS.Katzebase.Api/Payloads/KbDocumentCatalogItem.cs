namespace NTDLS.Katzebase.Api.Payloads
{
    public class KbDocumentCatalogItem
    {
        public uint Id { get; set; }

        public KbDocumentCatalogItem()
        {
        }

        public KbDocumentCatalogItem(uint id)
        {
            Id = id;
        }
    }
}
