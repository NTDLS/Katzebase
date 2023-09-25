using Katzebase.Engine.Indexes.Matching;

namespace Katzebase.Engine.Query.Constraints
{
    internal class ConditionSubset
    {
        public bool IsRoot { get; set; } = false;
        public string SubsetKey { get; set; }
        public string Expression { get; set; }
        public List<Condition> Conditions { get; set; } = new();
        public HashSet<string> SubsetKeys { get; set; } = new();
        public HashSet<string> ConditionKeys { get; set; } = new();

        /// <summary>
        /// If this condition is covered by an index, this is the index which we will use.
        /// </summary>
        public IndexSelection? IndexSelection { get; set; }

        public ConditionSubset(string subsetKey, string expression)
        {
            SubsetKey = subsetKey;
            Expression = expression;
        }
    }
}
