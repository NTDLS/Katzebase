namespace NTDLS.Katzebase.Api.Payloads
{
    public class KbActionResponseIndexes : KbBaseActionResponse
    {
        public List<KbIndex> Collection { get; set; } = new List<KbIndex>();

        public void Add(KbIndex value)
        {
            Collection.Add(value);
        }
    }
}
