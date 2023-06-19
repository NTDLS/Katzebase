namespace Katzebase.PublicLibrary.Payloads
{
    /// <summary>
    /// KbQueryResult is used to return a field-set and the associated row values.
    /// </summary>
    public class KbQueryResult : KbActionResponse
    {
        public List<KbQueryField> Fields { get; set; } = new();
        public List<KbQueryRow> Rows { get; set; } = new();

        public static KbQueryResult FromActionResponse(KbActionResponse actionResponse)
        {
            return new KbQueryResult()
            {
                RowCount = actionResponse.RowCount,
                Success = actionResponse.Success,
                Message = actionResponse.Message,
                Metrics = actionResponse.Metrics,
                Explanation = actionResponse.Explanation,
            };
        }
    }
}
