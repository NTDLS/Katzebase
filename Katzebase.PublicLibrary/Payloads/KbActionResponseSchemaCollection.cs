using static Katzebase.PublicLibrary.KbConstants;

namespace Katzebase.PublicLibrary.Payloads
{
    public class KbActionResponseSchemaCollection : KbIActionResponse
    {
        public List<KbSchemaItem> Collection { get; set; } = new();
        public List<KbQueryResultMessage> Messages { get; set; } = new();
        public HashSet<KbTransactionWarning> Warnings { get; set; } = new();
        public bool Success { get; set; } = true;
        public string? ExceptionText { get; set; }
        public KbMetricCollection? Metrics { get; set; }
        public string? Explanation { get; set; }
        public int RowCount { get; set; }
    }
}
