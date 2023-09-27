namespace NTDLS.Katzebase.Client.Payloads
{
    public class KbDocumentCatalogCollection : KbBaseActionResponse
    {
        public List<KbDocumentCatalogItem> Collection { get; set; } = new();
    }
}
