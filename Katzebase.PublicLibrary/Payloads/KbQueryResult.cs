namespace Katzebase.PublicLibrary.Payloads
{
    public class KbQueryResult : KbActionResponse
    {
        public List<KbQueryField> Fields { get; set; } = new();
        public List<KbQueryRow> Rows { get; set; } = new();

        public List<KbNameValue<double>> WaitTimes { get; set; } = new();

        public static KbQueryResult FromActionResponse(KbActionResponse actionResponse)
        {
            return new KbQueryResult()
            {
                Success = actionResponse.Success,
                Message = actionResponse.Message,
            };
        }
    }
}
