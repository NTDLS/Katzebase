using Newtonsoft.Json;
using System;

namespace Katzebase.Library.Payloads
{
    public class KbDocument
    {
        public Guid Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modfied { get; set; }
        public string Content { get; set; }

        public KbDocument()
        {
        }
        public KbDocument(object contentObject)
        {
            Content = JsonConvert.SerializeObject(contentObject);
        }
    }
}
