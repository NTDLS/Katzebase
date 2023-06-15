using static Katzebase.Engine.Indexes.Matching.IndexConstants;

namespace Katzebase.Engine.Indexes.Matching
{
    internal class IndexScanResult
    {
        public PhysicalIndexLeaf? Leaf { get; set; }
        public IndexMatchType MatchType { get; set; } = IndexMatchType.None;
        public int ExtentLevel { get; set; } = 0; //How far down the tree did we make it before we lost the path?
    }
}
