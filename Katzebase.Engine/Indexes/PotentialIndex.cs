namespace Katzebase.Engine.Indexes
{
    public class PotentialIndex
    {
        public List<string> CoveredFields { get; private set; }
        public PersistIndex Index { get; set; }
        public bool Tried { get; set; }
        public string CoveredHash => string.Join(":", CoveredFields.OrderBy(o => o)).ToLowerInvariant();

        public Guid SourceSubsetUID { get; private set; }

        public PotentialIndex(Guid sourceSubsetUID, PersistIndex index, List<string> coveredFields)
        {
            this.Index = index;
            this.CoveredFields = coveredFields;
        }
    }
}
