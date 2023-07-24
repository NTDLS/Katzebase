namespace Katzebase.PublicLibrary.Payloads
{
    public class KbActionResponse : KbIActionResponse
    {
        public bool Success { get; set; } = true;
        public string? ExceptionText { get; set; }
        public KbMetricCollection? Metrics { get; set; }
        public string? Explanation { get; set; }
        public int RowCount { get; set; }
        public List<KbQueryResultMessage> Messages { get; set; } = new();

        public KbActionResponse()
        {
        }

        public KbActionResponse(Exception ex)
        {
            ExceptionText = ex.Message;
            Success = false;
        }
    }
}
