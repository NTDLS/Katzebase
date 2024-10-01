using ProtoBuf;

namespace NTDLS.Katzebase.PersistentTypes.Document
{
    /// <summary>
    /// Contains the list of document IDs that exist in a page.
    /// </summary>
    [ProtoContract]
    [Serializable]
    public class PhysicalDocumentPageMap
    {
        [ProtoIgnore]
        private HashSet<uint>? _documentIDs;

        [ProtoMember(1)]
        public HashSet<uint> DocumentIDs
        {
            get => _documentIDs ??= new HashSet<uint>();
            set => _documentIDs = value;
        }

        public PhysicalDocumentPageMap()
        {
        }

        public int TotalDocumentCount()
            => DocumentIDs.Count;
    }
}
