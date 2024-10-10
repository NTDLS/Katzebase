using NTDLS.Katzebase.Api.Models;
using static NTDLS.Katzebase.Api.KbConstants;

namespace NTDLS.Katzebase.Api.Payloads.Response
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
