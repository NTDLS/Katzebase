using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Query.WhereAndJoinConditions;

namespace NTDLS.Katzebase.Engine.Indexes.Matching
{
    internal class IndexSelection<TData> where TData : IStringable
    {
        public HashSet<ConditionEntry> CoveredConditions { get; private set; } = new();

        public PhysicalIndex<TData> PhysicalIndex { get; private set; }

        /// <summary>
        /// When true, this means that we have all the fields we need to satisfy all index attributes for a index seek operation.
        /// </summary>
        public bool IsFullIndexMatch { get; set; } = false;

        public IndexSelection(PhysicalIndex<TData> index)
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

        public IndexSelection<TData> Clone()
        {
            var clone = new IndexSelection<TData>(PhysicalIndex)
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
