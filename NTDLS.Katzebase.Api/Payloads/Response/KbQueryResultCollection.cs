namespace NTDLS.Katzebase.Api.Payloads.Response
{
    /// <summary>
    /// KbQueryResult is used to return a collection of field-sets and their associated row values.
    /// </summary>
    public class KbQueryResultCollection : KbBaseActionResponse
    {
        public new double Duration => Collection.Sum(o => o.Duration);

        public List<KbQueryResult> Collection { get; set; } = new();

        public KbQueryResult AddNew()
        {
            var result = new KbQueryResult();
            Collection.Add(result);
            return result;
        }

        public void Add(KbQueryResultCollection result)
        {
            Collection.AddRange(result.Collection);
        }

        public void Add(KbQueryResult result)
        {
            Collection.Add(result);
        }

        public static KbQueryResult FromActionResponse(KbActionResponse actionResponse)
        {
            return new KbQueryResult()
            {
                RowCount = actionResponse.RowCount,
                Metrics = actionResponse.Metrics,
                Warnings = actionResponse.Warnings,
                Messages = actionResponse.Messages,
                Duration = actionResponse.Duration,
            };
        }
    }
}
