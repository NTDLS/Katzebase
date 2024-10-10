namespace NTDLS.Katzebase.Api.Payloads.Response
{
    public class KbActionResponseStrings : KbBaseActionResponse
    {
        public List<string> Values { get; set; } = new();

        public void Add(string value)
        {
            Values.Add(value);
        }
    }
}
