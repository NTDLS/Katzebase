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

        /// <summary>
        /// Tells us whether this page is currently compressed in memory of whether it has been expended to the dictionary.
        /// </summary>
        [ProtoIgnore]
        public bool IsMaterialized { get; private set; }

        [ProtoIgnore]
        public KbInsensitiveDictionary<string?> Elements
        {
            get
            {
                if (_dictionary == null)
                {
                    if (CompressedBytes == null)
                    {
                        throw new KbNullException("Document compressed bytes cannot be null.");
                    }

                    var serializedData = Library.Compression.Deflate.Decompress(CompressedBytes);

                    ContentLength = serializedData.Length;

                    using var input = new MemoryStream(serializedData);

                    _dictionary = Serializer.Deserialize<KbInsensitiveDictionary<string?>>(input) ??
                        throw new KbNullException("Document dictionary cannot be null.");

                    _compressedBytes = null; //For memory purposes, we want to store either compressed OR uncompressed - but not both.

                    IsMaterialized = true;
                }
                return _dictionary;
            }
        }

        [ProtoIgnore]
        private byte[]? _compressedBytes = null;

        [ProtoMember(1)]
        public byte[]? CompressedBytes
        {
            get
            {
                if (_compressedBytes == null)
                {
                    using var output = new MemoryStream();
                    Serializer.Serialize(output, _dictionary);
                    ContentLength = (int)output.Length;
                    _compressedBytes = Library.Compression.Deflate.Compress(output.ToArray());
                    _dictionary = null; //For memory purposes, we want to store either compressed OR uncompressed - but not both.
                    IsMaterialized = false;
                }

                return _compressedBytes;
            }
            set
            {
                _compressedBytes = value;
                _dictionary = null; //For memory purposes, we want to store either compressed OR uncompressed - but not both.
                IsMaterialized = false;
            }
        }

        [ProtoMember(2)]
        public DateTime Created { get; set; }

        [ProtoMember(3)]
        public DateTime Modified { get; set; }

        [ProtoMember(4)]
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
                _compressedBytes = _compressedBytes,
                _dictionary = _dictionary,
                Created = Created,
                Modified = Modified
            };
        }
    }
}
