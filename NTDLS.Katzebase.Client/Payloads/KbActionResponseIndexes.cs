namespace NTDLS.Katzebase.Client.Payloads
{
    public class KbActionResponseIndexes : KbBaseActionResponse
    {
        public List<KbIndex> List { get; set; } = new List<KbIndex>();

        public void Add(KbIndex value)
        {
            List.Add(value);
        }
    }
}
