using Katzebase.PublicLibrary.Exceptions;
using Newtonsoft.Json;

namespace Katzebase.Engine.Documents
{
    /// <summary>
    /// This is the page document that is physically written to the disk by
    ///     virture of being contained in the collection in PhysicalDocumentPage
    /// </summary>
    [Serializable]
    public class PhysicalDocument
    {
        public Dictionary<string, string> ToDictonary()
        {
            var documentContent = JsonConvert.DeserializeObject<Dictionary<string, string>>(Content);
            if (documentContent == null)
            {
                throw new KbNullException("Document dictinary cannot be null.");
            }
            return documentContent.ToDictionary(o => o.Key.ToLower(), o => o.Value);
        }

        public string Content { get; set; } = string.Empty;
        public uint Id { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modfied { get; set; }

        public PhysicalDocument Clone()
        {
            return new PhysicalDocument
            {
                Id = Id,
                Content = Content,
                Created = Created,
                Modfied = Modfied
            };
        }
    }
}
