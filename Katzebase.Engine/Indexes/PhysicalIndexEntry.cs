using ProtoBuf;

namespace Katzebase.Engine.Indexes
{
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
