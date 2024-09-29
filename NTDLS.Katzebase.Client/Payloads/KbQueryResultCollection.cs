namespace NTDLS.Katzebase.Client.Payloads
{
    /// <summary>
    /// KbQueryResult is used to return a collection of field-sets and their associated row values.
    /// </summary>
    public class KbQueryResultCollection : KbBaseActionResponse
    {
        public new double Duration => Collection.Sum(o => o.Duration);

        public List<KbQueryDocumentListResult> Collection { get; set; } = new();

        public KbQueryDocumentListResult AddNew()
        {
            var result = new KbQueryDocumentListResult();
            Collection.Add(result);
            return result;
        }

        public void Add(KbQueryResultCollection result)
        {
            Collection.AddRange(result.Collection);
        }

        public void Add(KbQueryDocumentListResult result)
        {
            Collection.Add(result);
        }

        public static KbQueryDocumentListResult FromActionResponse(KbActionResponse actionResponse)
        {
            return new KbQueryDocumentListResult()
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
