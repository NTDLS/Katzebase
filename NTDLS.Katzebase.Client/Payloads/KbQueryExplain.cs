namespace NTDLS.Katzebase.Client.Payloads
{
    /// <summary>
    /// KbQueryResult is used to return information on how a query will be executed.
    /// </summary>
    public class KbQueryExplain : KbBaseActionResponse
    {
        public static KbQueryExplain FromActionResponse(KbBaseActionResponse actionResponse)
        {
            return new KbQueryExplain()
            {
                Messages = actionResponse.Messages,
                RowCount = actionResponse.RowCount,
                Metrics = actionResponse.Metrics,
                Duration = actionResponse.Duration,
            };
        }
    }
}
