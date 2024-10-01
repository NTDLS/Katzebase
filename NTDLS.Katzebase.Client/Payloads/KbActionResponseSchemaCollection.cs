namespace NTDLS.Katzebase.Client.Payloads
{
    public class KbActionResponseSchemaCollection : KbBaseActionResponse
    {
        public List<KbSchema> Collection { get; set; } = new();
    }
}
