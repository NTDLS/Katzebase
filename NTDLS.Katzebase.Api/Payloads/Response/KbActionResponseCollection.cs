namespace NTDLS.Katzebase.Api.Payloads.Response
{
    public class KbActionResponseCollection : KbBaseActionResponse
    {
        public List<KbBaseActionResponse> Collection { get; set; } = new();

        public KbActionResponseCollection()
        {
        }

        public void Add(KbBaseActionResponse result)
        {
            Collection.Add(result);
        }
    }
}
