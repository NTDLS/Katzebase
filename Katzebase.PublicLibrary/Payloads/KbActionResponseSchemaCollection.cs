using static Katzebase.PublicLibrary.KbConstants;

namespace Katzebase.PublicLibrary.Payloads
{
    public class KbActionResponseSchemaCollection : KbBaseActionResponse
    {
        public List<KbSchemaItem> Collection { get; set; } = new();
    }
}
