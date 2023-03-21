using ProtoBuf;

namespace Katzebase.Engine.Indexes
{
    [ProtoContract]
    public class PersistIndexPageCatalog
    {
        [ProtoMember(1)]
        public PersistIndexLeaves Leaves = new PersistIndexLeaves();
    }

}
