using Newtonsoft.Json;
using ProtoBuf;

namespace Katzebase.Engine.Indexes
{
    [ProtoContract]
    public class PhysicalIndexLeaves
    {
        [ProtoMember(1)]
        public List<PhysicalIndexLeaf> Entries = new List<PhysicalIndexLeaf>();

        [JsonIgnore]
        public int Count
        {
            get
            {
                return Entries.Count;
            }
        }

        public PhysicalIndexLeaf AddNewleaf(string key)
        {
            var leaf = new PhysicalIndexLeaf(key);
            Entries.Add(leaf);
            return leaf;
        }

        public IEnumerator<PhysicalIndexLeaf> GetEnumerator()
        {
            int position = 0;
            while (position < Entries.Count)
            {
                yield return this[position++];
            }
        }

        public PhysicalIndexLeaf this[int index]
        {
            get
            {
                return Entries[index];
            }
        }
    }
}
