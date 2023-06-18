namespace Katzebase.PublicLibrary.Payloads
{
    public interface KbIActionResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public KbMetricCollection? Metrics { get; set; }
        public string? Explanation { get; set; }
        public int RowCount { get; set; }
    }
}
