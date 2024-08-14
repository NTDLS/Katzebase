using NTDLS.Katzebase.Engine.Query;

namespace NTDLS.Katzebase.Engine.Indexes.Matching
{
    public class IndexSelection
    {
        public HashSet<PrefixedField> CoveredFields { get; private set; } = new();
        public PhysicalIndex Index { get; private set; }

        public IndexSelection(PhysicalIndex index)
        {
            Index = index;
        }

        public override bool Equals(object? obj)
        {
            if (obj is PrefixedField other)
            {
                return Index.Name.Equals(other.Key);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Index.Name.GetHashCode();
        }
    }
}
