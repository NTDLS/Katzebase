using NTDLS.Katzebase.Api.Models;

namespace NTDLS.Katzebase.Api.Payloads.Response
{
    public class KbResponseFieldSampleCollection : KbBaseActionResponse
    {
        public List<KbResponseFieldSampleItem> Collection { get; set; } = new();
    }
}
