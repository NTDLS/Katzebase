namespace Katzebase.Engine.Indexes
{
    public class FindKeyPageResult
    {
        public PhysicalIndexPageCatalog? Catalog { get; set; }
        public PhysicalIndexLeaves? Leaves { get; set; }
        public PhysicalIndexLeaf? Leaf { get; set; }
        public bool IsFullMatch { get; set; }
        public bool IsPartialMatch { get; set; }
        public int ExtentLevel { get; set; } = 0;
    }
}
