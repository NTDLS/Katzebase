using Katzebase.Engine.Query;

namespace Katzebase.Engine.Indexes.Matching
{
    public class IndexSelection
    {
        public PhysicalIndex Index;
        /// <summary>
        /// The names of the document fileds that are covered by the index.
        /// </summary>
        public List<PrefixedField> CoveredFields { get; set; }

        public string CoveredHash => string.Join(":", CoveredFields.OrderBy(o => o)).ToLowerInvariant();

        public IndexSelection(PhysicalIndex index, List<PrefixedField> coveredFields)
        {
            CoveredFields = coveredFields;
            Index = index;
        }
    }
}
