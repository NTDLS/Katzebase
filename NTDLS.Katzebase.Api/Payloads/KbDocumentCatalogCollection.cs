namespace NTDLS.Katzebase.Api.Payloads
{
    public class KbDocumentCatalogCollection : KbBaseActionResponse
    {
        public List<KbDocumentCatalogItem> Collection { get; set; } = new();
    }
}
