using ProtoBuf;

namespace Katzebase.Engine.Indexes
{
    [ProtoContract]
    public class PhysicalIndexLeaf
    {
        [ProtoMember(1)]
        public string? Value { get; set; } = null;

        [ProtoMember(2)]
        public HashSet<Guid>? DocumentIDs = null;

        [ProtoMember(3)]
        public PhysicalIndexLeaves Leaves = new();

        public PhysicalIndexLeaf()
        {

        }

        public PhysicalIndexLeaf(string value)
        {
            Value = value;
        }
    }
}
