using ProtoBuf;

namespace Katzebase.Engine.Indexes
{
    [ProtoContract]
    public class PersistIndexLeaf
    {
        [ProtoMember(1)]
        public string? Key { get; set; } = null;

        [ProtoMember(2)]
        public HashSet<Guid>? DocumentIDs = null;

        [ProtoMember(3)]
        public PersistIndexLeaves Leaves = new();

        public PersistIndexLeaf()
        {

        }

        public PersistIndexLeaf(string key)
        {
            Key = key;
        }
    }
}
