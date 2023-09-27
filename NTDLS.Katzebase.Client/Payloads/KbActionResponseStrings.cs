namespace NTDLS.Katzebase.Client.Payloads
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
