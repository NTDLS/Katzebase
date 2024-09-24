using fs;
using NTDLS.Katzebase.Client.Types;
using ProtoBuf;

namespace NTDLS.Katzebase.Engine.Indexes
{
    //TODO: This should be a struct.
    [ProtoContract]
    public class PhysicalIndexLeaf
    {
        [ProtoMember(1)]
        public KbInsensitiveDictionary<fstring, PhysicalIndexLeaf> Children { get; set; } = new(fstring.CompareFunc);

        [ProtoMember(2)]
        public List<PhysicalIndexEntry>? Documents { get; set; } = null;

        public PhysicalIndexLeaf AddNewLeaf(fstring value)
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
