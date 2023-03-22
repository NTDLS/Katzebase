namespace Katzebase.Library.Payloads
{
    public class KbActionResponseStrings : KbActionResponse
    {
        public List<string> Values { get; set; } = new List<string>();

        public void Add(string value)
        {
            Values.Add(value);
        }
    }
}
