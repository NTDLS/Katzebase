using Newtonsoft.Json;

namespace Katzebase.PublicLibrary.Payloads
{
    public interface KbIActionResponse
    {
        [JsonProperty("Success")]

        public bool Success { get; set; }
        public string? ExceptionText { get; set; }
        public KbMetricCollection? Metrics { get; set; }
        public string? Explanation { get; set; }
        public int RowCount { get; set; }
    }
}
