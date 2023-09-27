namespace NTDLS.Katzebase.Client.Payloads
{
    /// <summary>
    /// KbQueryResult is used to return a field-set and the associated row values.
    /// </summary>
    public class KbQueryResult : KbBaseActionResponse
    {
        public List<KbQueryField> Fields { get; set; } = new();
        public List<KbQueryRow> Rows { get; set; } = new();

        public void AddField(string name)
        {
            Fields.Add(new KbQueryField(name));
        }

        public void AddRow(List<string?> values)
        {
            Rows.Add(new KbQueryRow(values));
        }

        public static KbQueryResult FromActionResponse(KbBaseActionResponse actionResponse)
        {
            return new KbQueryResult()
            {
                Messages = actionResponse.Messages,
                RowCount = actionResponse.RowCount,
                Success = actionResponse.Success,
                ExceptionText = actionResponse.ExceptionText,
                Metrics = actionResponse.Metrics,
                Explanation = actionResponse.Explanation,
                Duration = actionResponse.Duration,
            };
        }

        public KbQueryResultCollection ToCollection()
        {
            var result = new KbQueryResultCollection();
            result.Add(this);
            return result;
        }
    }
}
