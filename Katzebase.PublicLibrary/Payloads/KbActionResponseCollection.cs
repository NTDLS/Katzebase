namespace Katzebase.PublicLibrary.Payloads
{
    public class KbActionResponseCollection : KbActionResponse
    {
        public List<KbActionResponse> Collection { get; set; } = new();

        //public new List<KbQueryResultMessage> Messages => Collection.SelectMany(o => o.Messages).ToList();

        private bool _success = false;
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

        public void Add(KbActionResponse result)
        {
            Collection.Add(result);
        }

        public KbActionResponseCollection()
        {
        }

        public KbActionResponseCollection(Exception ex)
        {
            ExceptionText = ex.Message;
        }
    }
}
