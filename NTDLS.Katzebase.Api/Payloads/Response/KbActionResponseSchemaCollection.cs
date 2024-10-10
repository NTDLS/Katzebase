using NTDLS.Katzebase.Api.Models;

namespace NTDLS.Katzebase.Api.Payloads.Response
{
    public class KbActionResponseSchemaCollection : KbBaseActionResponse
    {
        public List<KbSchema> Collection { get; set; } = new();
    }
}
