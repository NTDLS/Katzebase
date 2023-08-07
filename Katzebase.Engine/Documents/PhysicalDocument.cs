using Katzebase.Engine.Library;
using Katzebase.PublicLibrary;
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

        [ProtoIgnore]
        private object _SetLock = new object();

        [ProtoMember(1)]
        public byte[]? _ContentBytes { get; set; } = null;

        public byte[] ContentBytes
        {
            get
            {
                lock (_SetLock)
                {
                    if (_ContentBytes == null)
                    {
                        KbUtility.EnsureNotNull(_Content);
                        _ContentBytes = Compression.Compress(_Content);
                    }
                    return _ContentBytes;
                }
            }
            set
            {
                lock (_SetLock)
                {
                    ContentLength = value.Length;
                    _ContentBytes = value;
                    _Content = null; //For memory purposes, we store EITHER the BYTES or the STRING - not both.
                }
            }
        }

        [ProtoIgnore]
        private string? _Content = null;

        [ProtoIgnore]
        public string Content
        {
            get
            {
                lock (_SetLock)
                {
                    if (_Content == null)
                    {
                        KbUtility.EnsureNotNull(ContentBytes);
                        _Content = Compression.DecompressString(ContentBytes);
                        _ContentBytes = null; //For memory purposes, we store EITHER the BYTES or the STRING - not both.
                    }
                    return _Content;
                }
            }
            set
            {
                lock (_SetLock)
                {
                    ContentLength = value.Length;
                    ContentBytes = Compression.Compress(value);
                }
            }
        }

        [ProtoMember(2)]
        public DateTime Created { get; set; }

        [ProtoMember(3)]
        public DateTime Modfied { get; set; }

        [ProtoMember(4)]
        public int ContentLength { get; set; }

        public PhysicalDocument Clone()
        {
            return new PhysicalDocument
            {
                Content = Content,
                Created = Created,
                Modfied = Modfied
            };
        }
    }
}
