using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dokdex.Engine.Indexes
{
    [ProtoContract]
    public class PersistIndexPageCatalog
    {
        [ProtoMember(1)]
        public PersistIndexLeaves Leaves = new PersistIndexLeaves();
    }

}
