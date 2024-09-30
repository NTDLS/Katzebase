using ProtoBuf;

namespace NTDLS.Katzebase.PersistentTypes.Index
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
