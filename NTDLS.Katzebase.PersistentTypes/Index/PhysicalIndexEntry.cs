using ProtoBuf;

namespace NTDLS.Katzebase.PersistentTypes.Index
{
    //TODO: This should be a struct.
    [ProtoContract]
    public class PhysicalIndexEntry
    {
        [ProtoMember(1)]
        public uint DocumentId { get; set; }

        [ProtoIgnore]
        public string Key => $"{DocumentId}";

        public PhysicalIndexEntry(uint documentId)
        {
            DocumentId = documentId;
        }

        public PhysicalIndexEntry()
        {
        }
    }
}
