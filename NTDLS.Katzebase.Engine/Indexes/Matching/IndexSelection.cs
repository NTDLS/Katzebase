using NTDLS.Katzebase.Engine.Query;

namespace NTDLS.Katzebase.Engine.Indexes.Matching
{
    public class IndexSelection
    {
        public PhysicalIndex PhysicalIndex;
        /// <summary>
        /// The names of the document fields that are covered by the index.
        /// </summary>
        public List<PrefixedField> CoveredFields { get; private set; }

        public string CoveredHash => string.Join(":", CoveredFields.OrderBy(o => o)).ToLowerInvariant();

        public IndexSelection(PhysicalIndex index, List<PrefixedField> coveredFields)
        {
            CoveredFields = coveredFields;
            PhysicalIndex = index;
        }
    }
}
