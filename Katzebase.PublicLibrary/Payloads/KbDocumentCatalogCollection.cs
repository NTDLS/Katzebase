namespace Katzebase.PublicLibrary.Payloads
{
    public class KbDocumentCatalogCollection : KbIActionResponse
    {
        public List<KbDocumentCatalogItem> Collection { get; set; } = new();
        public bool Success { get; set; }
        public string? ExceptionText { get; set; }
        public KbMetricCollection? Metrics { get; set; }
        public string? Explanation { get; set; }
        public int RowCount { get; set; }
    }
}
