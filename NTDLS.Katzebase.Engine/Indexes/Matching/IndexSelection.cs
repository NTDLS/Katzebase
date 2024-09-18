using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Query.WhereAndJoinConditions;

namespace NTDLS.Katzebase.Engine.Indexes.Matching
{
    internal class IndexSelection
    {
        public HashSet<ConditionEntry> CoveredConditions { get; private set; } = new();

        public PhysicalIndex PhysicalIndex { get; private set; }

        /// <summary>
        /// When true, this means that we have all the fields we need to satisfy all index attributes for a index seek operation.
        /// </summary>
        public bool IsFullIndexMatch { get; set; } = false;

        public IndexSelection(PhysicalIndex index)
        {
            PhysicalIndex = index;
        }

        public override bool Equals(object? obj)
        {
            if (obj is PrefixedField other)
            {
                return PhysicalIndex.Name.Equals(other.Key);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return PhysicalIndex.Name.GetHashCode();
        }

        public IndexSelection Clone()
        {
            var clone = new IndexSelection(PhysicalIndex)
            {
                IsFullIndexMatch = IsFullIndexMatch,
            };

            foreach (var condition in CoveredConditions)
            {
                CoveredConditions.Add(condition);
            }

            return clone;
        }
    }
}
