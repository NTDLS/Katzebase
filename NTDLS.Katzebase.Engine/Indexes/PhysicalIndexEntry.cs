using ProtoBuf;

namespace NTDLS.Katzebase.Engine.Indexes
{
    //TODO: This should be a struct.
    [ProtoContract]
    public class PhysicalIndexEntry
    {
        [ProtoMember(1)]
        public int PageNumber { get; private set; }

        [ProtoMember(2)]
        public uint DocumentId { get; set; }

        [ProtoIgnore]
        public string Key => $"{PageNumber}:{DocumentId}";

        public PhysicalIndexEntry(uint documentId, int pageNumber)
        {
            DocumentId = documentId;
            PageNumber = pageNumber;
        }

        public PhysicalIndexEntry()
        {
        }
    }
}
