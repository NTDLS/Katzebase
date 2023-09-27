using Newtonsoft.Json;

namespace NTDLS.Katzebase.Client.Payloads
{
    public class KbDocument
    {
        public uint Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modfied { get; set; }
        public string Content { get; set; } = string.Empty;

        public KbDocument()
        {
        }

        public KbDocument(object contentObject)
        {
            Content = JsonConvert.SerializeObject(contentObject);
        }
    }
}
