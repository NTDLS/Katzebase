using Katzebase.PublicLibrary.Exceptions;
using Katzebase.PublicLibrary.Payloads;
using Newtonsoft.Json;
using ProtoBuf;

namespace Katzebase.Engine.Documents
{
    /// <summary>
    /// This is the page document that is physically written to the disk by
    ///     virture of being contained in the collection in PhysicalDocumentPage
    /// </summary>
    [Serializable]
    [ProtoContract]
    public class PhysicalDocument
    {
        public CaseInSensitiveDictionary<string> ToDictonary()
        {
            var documentContent = JsonConvert.DeserializeObject<CaseInSensitiveDictionary<string>>(Content);
            if (documentContent == null)
            {
                throw new KbNullException("Document dictonary cannot be null.");
            }
            return documentContent;
        }

        [ProtoMember(1)]
        public string Content { get; set; } = string.Empty;
        [ProtoMember(2)] 
        public uint Id { get; set; }
        [ProtoMember(3)] 
        public DateTime Created { get; set; }
        [ProtoMember(4)] 
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
