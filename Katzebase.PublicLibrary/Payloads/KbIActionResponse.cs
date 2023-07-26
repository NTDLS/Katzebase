using Newtonsoft.Json;
using static Katzebase.PublicLibrary.KbConstants;

namespace Katzebase.PublicLibrary.Payloads
{
    public interface KbIActionResponse
    {
        public bool Success { get; set; }
        public string? ExceptionText { get; set; }
        public KbMetricCollection? Metrics { get; set; }
        public string? Explanation { get; set; }
        public int RowCount { get; set; }
        public List<KbQueryResultMessage> Messages { get; set; }
        public Dictionary<KbTransactionWarning, HashSet<string>> Warnings { get; set; }
    }
}
