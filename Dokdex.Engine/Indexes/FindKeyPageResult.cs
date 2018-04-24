using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dokdex.Engine.Indexes
{
    public class FindKeyPageResult
    {
        public PersistIndexPageCatalog Catalog { get; set; }
        public PersistIndexLeaves Leaves { get; set; }
        public PersistIndexLeaf Leaf { get; set; }
        public bool IsFullMatch { get; set; }
        public bool IsPartialMatch { get; set; }
        public int ExtentLevel { get; set; }
    }
}
