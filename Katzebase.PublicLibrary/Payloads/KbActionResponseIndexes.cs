namespace Katzebase.PublicLibrary.Payloads
{
    public class KbActionResponseIndexes : KbActionResponse
    {
        public List<KbIndex> List { get; set; } = new List<KbIndex>();

        public void Add(KbIndex value)
        {
            List.Add(value);
        }
    }
}
