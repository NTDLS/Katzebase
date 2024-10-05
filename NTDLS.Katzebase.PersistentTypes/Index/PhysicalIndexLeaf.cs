using NTDLS.Katzebase.Api.Types;
using ProtoBuf;

namespace NTDLS.Katzebase.PersistentTypes.Index
{
    //TODO: This should be a struct.
    [ProtoContract]
    public class PhysicalIndexLeaf
    {
        [ProtoMember(1)]
        public KbInsensitiveDictionary<PhysicalIndexLeaf> Children { get; set; } = new();

        [ProtoMember(2)]
        public List<PhysicalIndexEntry>? Documents { get; set; } = null;

        public PhysicalIndexLeaf AddNewLeaf(string value)
        {
            var newLeaf = new PhysicalIndexLeaf();
            Children.Add(value, newLeaf);
            return newLeaf;
        }

        public PhysicalIndexLeaf()
        {
        }
    }
}
