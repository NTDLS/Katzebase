using static NTDLS.Katzebase.Client.KbConstants;

namespace NTDLS.Katzebase.Client.Payloads
{
    public class KbBaseActionResponse
    {
        public KbMetricCollection? Metrics { get; set; }
        public int RowCount { get; set; }
        public List<KbQueryResultMessage> Messages { get; set; } = new();
        public Dictionary<KbTransactionWarning, HashSet<string>> Warnings { get; set; } = new();
        public double Duration { get; set; } = 0;
    }
}
