using NTDLS.Katzebase.Parsers.Interfaces;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Query.WhereAndJoinConditions;
using NTDLS.Katzebase.Parsers.Interfaces;
namespace NTDLS.Katzebase.Parsers.Indexes.Matching
{
    public class IndexSelection<TData> where TData : IStringable
    {
        public HashSet<ConditionEntry<TData>> CoveredConditions { get; private set; } = new();

        public IPhysicalIndex<TData> PhysicalIndex { get; private set; }

        /// <summary>
        /// When true, this means that we have all the fields we need to satisfy all index attributes for a index seek operation.
        /// </summary>
        public bool IsFullIndexMatch { get; set; } = false;

        public IndexSelection(IPhysicalIndex<TData> index)
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
