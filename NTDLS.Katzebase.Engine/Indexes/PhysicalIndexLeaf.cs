using NTDLS.Katzebase.Client.Types;
using ProtoBuf;

namespace NTDLS.Katzebase.Engine.Indexes
{
    //TODO: This should be a struct.
    [ProtoContract]
    public class PhysicalIndexLeaf<TData> where TData : IStringable
    {
        [ProtoMember(1)]
        public KbInsensitiveDictionary<PhysicalIndexLeaf<TData>> Children { get; set; } = new();

        [ProtoMember(2)]
        public List<PhysicalIndexEntry>? Documents { get; set; } = null;

        public PhysicalIndexLeaf<TData> AddNewLeaf(TData value)
        {
            var newLeaf = new PhysicalIndexLeaf<TData>();
            Children.Add(value.ToKey(), newLeaf);
            return newLeaf;
        }

        public PhysicalIndexLeaf()
        {
        }
    }
}
