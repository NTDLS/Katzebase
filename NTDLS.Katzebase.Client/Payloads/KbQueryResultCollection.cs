namespace NTDLS.Katzebase.Client.Payloads
{
    /// <summary>
    /// KbQueryResult is used to return a collection of field-sets and their associated row values.
    /// </summary>
    public class KbQueryResultCollection : KbBaseActionResponse
    {
        //public new List<KbQueryResultMessage> Messages => Collection.SelectMany(o => o.Messages).ToList();
        //public new Dictionary<KbTransactionWarning, HashSet<string>> Warnings => Collection.SelectMany(o => o.Warnings).ToDictionary(o => o.Key, o => o.Value);
        //public new int RowCount => Collection.Sum(o => o.RowCount);
        public new double Duration => Collection.Sum(o => o.Duration);

        public List<KbQueryResult> Collection { get; set; } = new();

        private bool _success = true;
        public new bool Success
        {
            get
            {
                return _success && Collection.All(o => o.Success);
            }
            set
            {
                _success = value;
            }
        }

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
                Success = actionResponse.Success,
                ExceptionText = actionResponse.ExceptionText,
                Metrics = actionResponse.Metrics,
                Explanation = actionResponse.Explanation,
            };
        }
    }
}
