using ProtoBuf;

namespace NTDLS.Katzebase.Engine.Indexes
{
    [ProtoContract]
    public class PhysicalIndexPages
    {
        [ProtoMember(1)]
        internal PhysicalIndexLeaf Root = new();

        public PhysicalIndexPages()
        {
        }
    }
}
