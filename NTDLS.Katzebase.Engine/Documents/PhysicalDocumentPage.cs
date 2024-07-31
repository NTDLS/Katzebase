using ProtoBuf;

namespace NTDLS.Katzebase.Engine.Documents
{
    /// <summary>
    /// This is the actual file that gets saved for each document page. It contains the
    ///     page number and a dictionary of the document IDs along with the actual document data.
    /// </summary>
    [ProtoContract]
    [Serializable]
    public class PhysicalDocumentPage
    {
        public PhysicalDocumentPage() { }

        [ProtoIgnore]
        private Dictionary<uint, PhysicalDocument>? _documents;

        [ProtoMember(1)]
        public Dictionary<uint, PhysicalDocument> Documents
        {
            get
            {
                _documents ??= new Dictionary<uint, PhysicalDocument>();
                return _documents;
            }
            set
            {
                _documents = value;
            }
        }
    }
}
