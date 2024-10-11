using NTDLS.Katzebase.Parsers.Query.Conditions;
using NTDLS.Katzebase.PersistentTypes.Index;

namespace NTDLS.Katzebase.Parsers.Indexes.Matching
{
    public class IndexSelection(PhysicalIndex index)
    {
        public HashSet<ConditionEntry> CoveredConditions { get; private set; } = new();

        public PhysicalIndex PhysicalIndex { get; private set; } = index;

        /// <summary>
        /// When true, this means that we have all the fields we need to satisfy all index attributes for a index seek operation.
        /// </summary>
        public bool IsFullIndexMatch { get; set; } = false;

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
