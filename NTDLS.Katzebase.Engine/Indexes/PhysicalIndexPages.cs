using ProtoBuf;

namespace NTDLS.Katzebase.Engine.Indexes
{
    [ProtoContract]
    internal class PhysicalIndexPages
    {
        [ProtoMember(1)]
        internal PhysicalIndexLeaf Root = new();

        public PhysicalIndexPages()
        {
        }
    }
}
