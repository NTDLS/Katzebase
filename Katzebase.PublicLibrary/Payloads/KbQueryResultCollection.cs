using static Katzebase.PublicLibrary.KbConstants;

namespace Katzebase.PublicLibrary.Payloads
{
    /// <summary>
    /// KbQueryResult is used to return a field-set and the associated row values.
    /// </summary>
    public class KbQueryResultCollection : KbActionResponse
    {
        public List<KbQueryResult> Collection { get; set; } = new();
        public new List<KbQueryResultMessage> Messages => Collection.SelectMany(o => o.Messages).ToList();
        public new int RowCount => Collection.Sum(o => o.RowCount);

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

        public void AddMessage(string text, KbMessageType type)
        {
            Messages.Add(new KbQueryResultMessage(text, type));
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
