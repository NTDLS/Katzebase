namespace Katzebase.Engine.Indexes
{
    public class IndexSelection
    {
        public PhysicalIndex Index;
        /// <summary>
        /// The names of the document fileds that are covered by the index.
        /// </summary>
        public List<string> CoveredFields { get; set; }

        public string CoveredHash => string.Join(":", CoveredFields.OrderBy(o => o)).ToLowerInvariant();

        public IndexSelection(PhysicalIndex index, List<string> coveredFields)
        {
            CoveredFields = coveredFields;
            Index = index;
        }
    }
}
