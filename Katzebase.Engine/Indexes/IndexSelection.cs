namespace Katzebase.Engine.Indexes
{
    public class IndexSelection
    {
        public PersistIndex Index;
        /// <summary>
        /// The names of the document fileds that are covered by the index.
        /// </summary>
        public List<string> CoveredFields { get; set; }

        public IndexSelection(PersistIndex index, List<string> coveredFields)
        {
            this.CoveredFields = coveredFields;
            this.Index = index;
        }
    }
}
