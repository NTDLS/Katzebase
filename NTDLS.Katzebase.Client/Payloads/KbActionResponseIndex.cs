namespace NTDLS.Katzebase.Client.Payloads
{
    public class KbActionResponseIndex : KbBaseActionResponse
    {
        public KbIndex? Index { get; set; }

        public KbActionResponseIndex()
        {
        }

        public KbActionResponseIndex(KbIndex? index)
        {
            Index = index;
        }
    }
}
