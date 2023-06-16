using Katzebase.Engine.Query;

namespace Katzebase.Engine.Indexes.Matching
{
    public class PotentialIndex
    {
        public List<PrefixedField> CoveredFields { get; private set; }
        public PhysicalIndex Index { get; set; }
        public bool Tried { get; set; }
        public string CoveredHash => string.Join(":", CoveredFields.OrderBy(o => o)).ToLowerInvariant();
        public Guid SourceSubsetUID { get; private set; }

        public PotentialIndex(PhysicalIndex index, List<PrefixedField> coveredFields)
        {
            Index = index;
            CoveredFields = coveredFields;
        }
    }
}
