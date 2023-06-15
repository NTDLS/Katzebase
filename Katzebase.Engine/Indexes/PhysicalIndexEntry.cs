using ProtoBuf;

namespace Katzebase.Engine.Indexes
{
    [ProtoContract]
    public class PhysicalIndexEntry
    {
        [ProtoMember(1)]
        public int PageNumber { get; private set; }

        [ProtoMember(2)]
        public Guid Id { get; set; }

        public PhysicalIndexEntry(Guid id, int pageNumber)
        {
            Id = id;
            PageNumber = pageNumber;
        }

        public PhysicalIndexEntry()
        {
        }
    }
}
