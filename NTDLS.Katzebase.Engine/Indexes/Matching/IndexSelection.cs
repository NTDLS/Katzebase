using NTDLS.Katzebase.Engine.Query;
using NTDLS.Katzebase.Engine.Query.Constraints;

namespace NTDLS.Katzebase.Engine.Indexes.Matching
{
    internal class IndexSelection
    {
        public HashSet<Condition> CoveredConditions { get; private set; } = new();
        public PhysicalIndex Index { get; private set; }

        /// <summary>
        /// When true, this means that we have all the fields we need to satisfy all index attributes for a index seek operation.
        /// </summary>
        public bool IsFullIndexMatch { get; set; } = false;

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
