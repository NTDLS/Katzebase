using static Katzebase.PublicLibrary.KbConstants;

namespace Katzebase.PublicLibrary.Payloads
{
    public class KbBaseActionResponse : KbIActionResponse
    {
        public bool Success { get; set; } = true;
        public string? ExceptionText { get; set; }
        public KbMetricCollection? Metrics { get; set; }
        public string? Explanation { get; set; }
        public int RowCount { get; set; }
        public List<KbQueryResultMessage> Messages { get; set; } = new();
        public Dictionary<KbTransactionWarning, HashSet<string>> Warnings { get; set; } = new();

        public KbBaseActionResponse()
        {
        }

        public KbBaseActionResponse(Exception ex)
        {
            ExceptionText = ex.Message;
            Success = false;
        }
    }
}
