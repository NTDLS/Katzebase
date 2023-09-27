namespace NTDLS.Katzebase.Client.Payloads
{
    public class KbActionResponseSchemaCollection : KbBaseActionResponse
    {
        public List<KbSchemaItem> Collection { get; set; } = new();
    }
}
