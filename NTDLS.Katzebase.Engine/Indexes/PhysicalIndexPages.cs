using ProtoBuf;

namespace NTDLS.Katzebase.Engine.Indexes
{
    [ProtoContract]
    public class PhysicalIndexPages
    {
        [ProtoMember(1)]
        public PhysicalIndexLeaf Root = new();

        public PhysicalIndexPages()
        {
        }
    }
}
