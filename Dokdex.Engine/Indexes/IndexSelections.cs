using System.Collections.Generic;

namespace Dokdex.Engine.Indexes
{
    public class IndexSelections: List<IndexSelection>
    {
        public List<string> UnhandledKeys { get; set; }

        public IndexSelections()
        {
            UnhandledKeys = new List<string>();
        }
    }
}
