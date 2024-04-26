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

        [ProtoMember(1)]
        public Dictionary<uint, PhysicalDocument> Documents { get; set; } = new();
    }
}
