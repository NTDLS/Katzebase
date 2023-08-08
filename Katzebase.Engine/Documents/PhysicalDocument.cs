using Katzebase.PublicLibrary;
using Katzebase.PublicLibrary.Payloads;
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
        public KBCILookup<string?> Dictonary = new();
        /*
        [ProtoIgnore]
        private KBCILookup<string?>? _dictonary = null;

        [ProtoIgnore]
        public KBCILookup<string?> Dictonary
        {
            get
            {
                if (_dictonary == null)
                {
                    if (CompressedBytes == null)
                    {
                        throw new KbNullException("Document compressed bytes cannot be null.");
                    }

                    var serializedData = Compression.Decompress(CompressedBytes);
                    ContentLength = serializedData.Length;
                    using (var input = new MemoryStream(serializedData))
                    {
                        _dictonary = Serializer.Deserialize<KBCILookup<string?>>(input);
                        if (_dictonary == null)
                        {
                            throw new KbNullException("Document dictonary cannot be null.");
                        }
                        //TODO: Maybe theres a more optimistic way to do this. Other than RAM, there is no need to NULL out the other property
                        //TODO:     This could lead to us de/serialize and de/compressing multiple times if we need to write a document.
                        _compressedBytes = null; //For memory purposes, we want to store either compressed OR uncompressed - but not both.
                    }
                }
                return _dictonary;
            }
        }

        public byte[]? _compressedBytes = null;

        [ProtoMember(1)]
        public byte[]? CompressedBytes
        {
            get
            {
                if (_compressedBytes == null)
                {
                    using (var output = new MemoryStream())
                    {
                        Serializer.Serialize(output, _dictonary);
                        ContentLength = (int)output.Length;
                        _compressedBytes = Compression.Compress(output.ToArray());
                        //TODO: Maybe theres a more optimistic way to do this. Other than RAM, there is no need to NULL out the other property
                        //TODO:     This could lead to us de/serialize and de/compressing multiple times if we need to write a document.
                        _dictonary = null; //For memory purposes, we want to store either compressed OR uncompressed - but not both.
                    }
                }

                return _compressedBytes;
            }
            set
            {
                _compressedBytes = value;
                //TODO: Maybe theres a more optimistic way to do this. Other than RAM, there is no need to NULL out the other property
                //TODO:     This could lead to us de/serialize and de/compressing multiple times if we need to write a document.
                _dictonary = null; //For memory purposes, we want to store either compressed OR uncompressed - but not both.
            }
        }
        */

        [ProtoMember(2)]
        public DateTime Created { get; set; }

        [ProtoMember(3)]
        public DateTime Modfied { get; set; }

        [ProtoMember(4)]
        public int ContentLength { get; set; }

        public PhysicalDocument()
        {
        }

        public PhysicalDocument(string jsonString)
        {
            SetDictonaryByJson(jsonString);
        }

        public void SetDictonaryByJson(string jsonString)
        {
            var dictonary = JsonConvert.DeserializeObject<KBCILookup<string?>>(jsonString);
            KbUtility.EnsureNotNull(dictonary);
            Dictonary = dictonary;
        }

        public PhysicalDocument Clone()
        {
            return new PhysicalDocument
            {
                Dictonary = Dictonary,
                Created = Created,
                Modfied = Modfied
            };
        }
    }
}
