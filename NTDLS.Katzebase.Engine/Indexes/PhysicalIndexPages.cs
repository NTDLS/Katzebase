using ProtoBuf;

namespace NTDLS.Katzebase.Engine.Indexes
{
    //TODO: This should be a struct.
    [ProtoContract]
    public class PhysicalIndexPages<TData> where TData : IStringable
    {
        [ProtoMember(1)]
        public PhysicalIndexLeaf<TData> Root = new();

        public PhysicalIndexPages()
        {
        }
    }
}
