using Newtonsoft.Json;
using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Exceptions;
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
        [ProtoIgnore]
        private KbInsensitiveDictionary<string?>? _dictionary = null;

        [ProtoIgnore]
        public KbInsensitiveDictionary<string?> Elements
        {
            get
            {
                if (_dictionary == null)
                {
                    throw new KbNullException("Document dictionary cannot be null.");
                }
                return _dictionary;
            }
        }

        [ProtoMember(1)]
        public byte[]? CompressedBytes
        {
            get
            {
                //This property getter is only called at serialization.

                using var output = new MemoryStream();
                Serializer.Serialize(output, _dictionary);
                ContentLength = (int)output.Length;
                return Library.Compression.Deflate.Compress(output.ToArray());
            }
            set
            {
                //This property setter is only called at deserialization.

                if (value == null)
                {
                    throw new KbNullException("Document compressed bytes cannot be null.");
                }

                var serializedData = Library.Compression.Deflate.Decompress(value);
                ContentLength = serializedData.Length;

                using var input = new MemoryStream(serializedData);

                _dictionary = Serializer.Deserialize<KbInsensitiveDictionary<string?>>(input) ??
                    throw new KbNullException("Document dictionary cannot be null.");
            }
        }

        [ProtoMember(2)]
        public DateTime Created { get; set; }

        [ProtoMember(3)]
        public DateTime Modified { get; set; }

        [ProtoIgnore]
        public int ContentLength { get; set; }

        public PhysicalDocument()
        {
        }

        public PhysicalDocument(string jsonString)
        {
            SetElementsByJson(jsonString);
        }

        public void SetElementsByJson(string jsonString)
        {
            var dictionary = JsonConvert.DeserializeObject<KbInsensitiveDictionary<string?>>(jsonString);
            _dictionary = dictionary.EnsureNotNull();
        }

        public PhysicalDocument Clone()
        {
            return new PhysicalDocument
            {
                _dictionary = _dictionary,
                Created = Created,
                Modified = Modified
            };
        }
    }
}
