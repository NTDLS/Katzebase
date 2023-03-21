using Newtonsoft.Json;
using System;

namespace Katzebase.Library.Payloads
{
    public class Document
    {
        public Guid Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modfied { get; set; }
        public string Content { get; set; }

        public Document()
        {
        }
        public Document(object contentObject)
        {
            Content = JsonConvert.SerializeObject(contentObject);
        }
    }
}
