using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dokdex.Engine.Indexes
{
    [ProtoContract]
    public class PersistIndexLeaf
    {
        [ProtoMember(1)]
        public string Key { get; set; }
        [ProtoMember(2)]
        public HashSet<Guid> DocumentIDs = null;
        [ProtoMember(3)]
        public PersistIndexLeaves Leaves = new PersistIndexLeaves();

        public PersistIndexLeaf()
        {

        }

        public PersistIndexLeaf(string key)
        {
            Key = key;
        }
    }
}
