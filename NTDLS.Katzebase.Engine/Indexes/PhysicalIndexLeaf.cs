using NTDLS.Katzebase.Client.Types;
using ProtoBuf;

namespace NTDLS.Katzebase.Engine.Indexes
{
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
