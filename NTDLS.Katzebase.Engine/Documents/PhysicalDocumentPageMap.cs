using ProtoBuf;

namespace NTDLS.Katzebase.Engine.Documents
{
    /// <summary>
    /// Contains the list of document IDs that exist in a page.
    /// </summary>
    [ProtoContract]
    [Serializable]
    public class PhysicalDocumentPageMap
    {
        public PhysicalDocumentPageMap() { }

        [ProtoMember(1)]
        public HashSet<uint> DocumentIDs { get; private set; } = new();

        public int TotalDocumentCount()
        {
            return DocumentIDs.Count;
        }
    }
}
