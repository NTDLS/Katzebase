namespace Katzebase.PublicLibrary.Payloads
{
    public class KbActionResponseSchemas : KbActionResponse
    {
        public List<KbSchema> List { get; set; } = new List<KbSchema>();

        public void Add(KbSchema value)
        {
            List.Add(value);
        }
    }
}
