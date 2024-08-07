using NTDLS.Katzebase.Engine.Query;

namespace NTDLS.Katzebase.Engine.Indexes.Matching
{
    public class PotentialIndex
    {
        public List<PrefixedField> CoveredFields { get; private set; }
        public PhysicalIndex Index { get; private set; }
        public bool Tried { get; private set; }
        public string CoveredHash => string.Join(":", CoveredFields.OrderBy(o => o)).ToLowerInvariant();
        public Guid SourceSubConditionUID { get; private set; }

        public void SetTried()
        {
            Tried = true;
        }

        public PotentialIndex(PhysicalIndex index, List<PrefixedField> coveredFields)
        {
            Index = index;
            CoveredFields = coveredFields;
        }
    }
}
