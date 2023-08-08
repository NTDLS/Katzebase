using Katzebase.PublicLibrary.Types;
using ProtoBuf;

namespace Katzebase.Engine.Indexes
{
    [ProtoContract]
    internal class PhysicalIndexLeaf
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
