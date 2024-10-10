using NTDLS.Katzebase.Api.Models;

namespace NTDLS.Katzebase.Api.Payloads.Response
{
    public class KbDocumentCatalogCollection : KbBaseActionResponse
    {
        public List<KbDocumentCatalogItem> Collection { get; set; } = new();
    }
}
