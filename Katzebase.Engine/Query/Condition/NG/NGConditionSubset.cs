using Katzebase.Engine.Indexes;

namespace Katzebase.Engine.Query.Condition.NG
{
    public class NGConditionSubset
    {
        public string SubsetKey { get; set; }
        public string Expression { get; set; }
        public List<NGCondition> Conditions { get; set; } = new();

        /// <summary>
        /// If this condition is covered by an index, this is the index which we will use.
        /// </summary>
        public IndexSelection? IndexSelection { get; set; }

        public NGConditionSubset(string subsetKey, string expression)
        {
            SubsetKey = subsetKey;
            Expression = expression;
        }
    }
}
