using static Katzebase.PublicLibrary.KbConstants;

namespace Katzebase.PublicLibrary.Payloads
{
    public class KbDocumentCatalogCollection : KbIActionResponse
    {
        public List<KbDocumentCatalogItem> Collection { get; set; } = new();
        public List<KbQueryResultMessage> Messages { get; set; } = new();
        public Dictionary<KbTransactionWarning, HashSet<string>> Warnings { get; set; } = new();
        public bool Success { get; set; } = true;
        public string? ExceptionText { get; set; }
        public KbMetricCollection? Metrics { get; set; }
        public string? Explanation { get; set; }
        public int RowCount { get; set; }
    }
}
