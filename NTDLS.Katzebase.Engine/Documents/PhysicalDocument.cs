using Newtonsoft.Json;
using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Types;
using ProtoBuf;

namespace NTDLS.Katzebase.Engine.Documents
{
    /// <summary>
    /// This is the page document that is physically written to the disk by virtue of being contained in the collection in PhysicalDocumentPage.
    /// </summary>
    [Serializable]
    [ProtoContract]
    public class PhysicalDocument
    {
        [ProtoMember(1)]
        public KbInsensitiveDictionary<string?> Elements { get; set; }

        [ProtoMember(2)]
        public DateTime Created { get; set; }

        [ProtoMember(3)]
        public DateTime Modified { get; set; }

        [ProtoIgnore]
        public int ContentLength { get; set; }

        public PhysicalDocument()
        {
            Elements = new();
        }

        public PhysicalDocument(string jsonString)
        {
            Elements = JsonConvert.DeserializeObject<KbInsensitiveDictionary<string?>>(jsonString).EnsureNotNull();
        }

        public void SetElementsByJson(string jsonString)
        {
            Elements = JsonConvert.DeserializeObject<KbInsensitiveDictionary<string?>>(jsonString).EnsureNotNull();
        }

        public PhysicalDocument Clone()
        {
            return new PhysicalDocument
            {
                Elements = Elements,
                Created = Created,
                Modified = Modified
            };
        }
    }
}
