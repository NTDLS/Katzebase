namespace Katzebase.PublicLibrary.Payloads
{
    public class KbDocumentCatalogCollection : List<KbDocumentCatalogItem>, KbIActionResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public List<KbNameValue<double>>? WaitTimes { get; set; }
        public string? Explanation { get; set; }
    }
}
