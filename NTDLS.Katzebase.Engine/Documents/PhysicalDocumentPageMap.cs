using ProtoBuf;

namespace NTDLS.Katzebase.Engine.Documents
{
    /// <summary>
    /// Contains the list of document IDs that exist in a page.
    /// </summary>
    [ProtoContract]
    [Serializable]
    public struct PhysicalDocumentPageMap
    {
        public PhysicalDocumentPageMap() { }

        [ProtoIgnore]
        private HashSet<uint>? _documentIDs;

        [ProtoMember(1)]
        public HashSet<uint> DocumentIDs
        {
            get
            {
                _documentIDs ??= new HashSet<uint>();
                return _documentIDs;
            }
            set
            {
                _documentIDs = value;
            }
        }

        public int TotalDocumentCount()
        {
            return DocumentIDs.Count;
        }
    }
}
