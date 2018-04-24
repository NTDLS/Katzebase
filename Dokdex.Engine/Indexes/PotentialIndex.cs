using System.Collections.Generic;

namespace Dokdex.Engine.Indexes
{
    public class PotentialIndex
    {
        public List<string> HandledKeyNames { get; set; }
        public PersistIndex Index { get; set; }
        public bool Tried { get; set; }

        public PotentialIndex(PersistIndex index, List<string> handledKeyNames)
        {
            this.Index = index;
            this.HandledKeyNames = handledKeyNames;
        }
    }
}
