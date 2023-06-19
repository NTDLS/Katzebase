namespace Katzebase.PublicLibrary.Payloads
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
