using ProtoBuf;

namespace NTDLS.Katzebase.Engine.Indexes
{
    //TODO: This should be a struct.
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
