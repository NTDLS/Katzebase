using Katzebase.PublicLibrary;
using Katzebase.PublicLibrary.Types;
using Newtonsoft.Json;
using ProtoBuf;

namespace Katzebase.Engine.Documents
{
    /// <summary>
    /// This is the page document that is physically written to the disk by virture of being contained in the collection in PhysicalDocumentPage.
    /// </summary>
    [Serializable]
    [ProtoContract]
    public class PhysicalDocument
    {
        [ProtoMember(1)]
        public KbInsensitiveDictionary<string?> Dictionary = new();

        [ProtoMember(2)]
        public DateTime Created { get; set; }

        [ProtoMember(3)]
        public DateTime Modfied { get; set; }

        [ProtoIgnore]
        public int ContentLength => Dictionary.Sum(o => o.Key.Length + (o.Value?.Length ?? 0));

        public PhysicalDocument()
        {
        }

        public PhysicalDocument(string jsonString)
        {
            var dictionary = JsonConvert.DeserializeObject<KbInsensitiveDictionary<string?>>(jsonString);
            KbUtility.EnsureNotNull(dictionary);
            Dictionary = dictionary;
        }

        public PhysicalDocument Clone()
        {
            return new PhysicalDocument
            {
                Dictionary = Dictionary,
                Created = Created,
                Modfied = Modfied
            };
        }
    }
}
