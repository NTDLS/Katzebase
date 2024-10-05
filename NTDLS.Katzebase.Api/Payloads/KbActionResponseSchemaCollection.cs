namespace NTDLS.Katzebase.Api.Payloads
{
    public class KbActionResponseSchemaCollection : KbBaseActionResponse
    {
        public List<KbSchema> Collection { get; set; } = new();
    }
}
