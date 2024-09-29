namespace NTDLS.Katzebase.Client.Payloads
{
    /// <summary>
    /// KbQueryResult is used to return information on how a collection of queries will be executed.
    /// </summary>
    public class KbQueryExplainCollection : KbBaseActionResponse
    {
        public new double Duration => Collection.Sum(o => o.Duration);

        public List<KbQueryExplain> Collection { get; set; } = new();

        public KbQueryExplain AddNew()
        {
            var result = new KbQueryExplain();
            Collection.Add(result);
            return result;
        }

        public void Add(KbQueryExplainCollection result)
        {
            Collection.AddRange(result.Collection);
        }

        public void Add(KbQueryExplain result)
        {
            Collection.Add(result);
        }

        public static KbQueryExplain FromActionResponse(KbActionResponse actionResponse)
        {
            return new KbQueryExplain()
            {
                RowCount = actionResponse.RowCount,
                Metrics = actionResponse.Metrics,
                Duration = actionResponse.Duration,
                Messages = actionResponse.Messages,
                Warnings = actionResponse.Warnings
            };
        }
    }
}
