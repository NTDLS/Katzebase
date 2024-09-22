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
        private int? _contentLength { get; set; } = null;

        [ProtoIgnore]
        public int ContentLength
        {
            get
            {
                _contentLength ??= JsonConvert.SerializeObject(Elements).Length;
                return (int)_contentLength;
            }
        }

        public PhysicalDocument()
        {
            Elements = new();
        }

        public PhysicalDocument(string jsonString)
        {
            _contentLength = jsonString.Length;
            Elements = JsonConvert.DeserializeObject<KbInsensitiveDictionary<string?>>(jsonString).EnsureNotNull();
        }

        public void SetElementsByJson(string jsonString)
        {
            _contentLength = jsonString.Length;
            Elements = JsonConvert.DeserializeObject<KbInsensitiveDictionary<string?>>(jsonString).EnsureNotNull();
        }
    }
}
