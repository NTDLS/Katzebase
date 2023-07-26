namespace Katzebase.PublicLibrary.Payloads
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
