using Newtonsoft.Json;
using ProtoBuf;
using System.Collections;

namespace Katzebase.Engine.Indexes
{
    [ProtoContract]
    public class PhysicalIndexLeaves : IEnumerable<PhysicalIndexLeaf>
    {
        [ProtoMember(1)]
        public List<PhysicalIndexLeaf> Entries = new();

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

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
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
