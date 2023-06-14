using ProtoBuf;

namespace Katzebase.Engine.Indexes
{
    [ProtoContract]
    public class PhysicalIndexPageCatalog
    {
        [ProtoMember(1)]
        public PhysicalIndexLeaves Leaves = new PhysicalIndexLeaves();
    }

}
