using System.Collections.Generic;

namespace Dokdex.Engine.Indexes
{
    public class IndexSelection
    {
        public PersistIndex Index;
        public List<string> HandledKeyNames { get; set; }

        public IndexSelection(PersistIndex index, List<string> handledKeyNames)
        {
            this.HandledKeyNames = handledKeyNames;
            this.Index = index;
        }
    }
}
