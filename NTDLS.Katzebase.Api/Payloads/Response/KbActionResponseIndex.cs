using NTDLS.Katzebase.Api.Models;

namespace NTDLS.Katzebase.Api.Payloads.Response
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
