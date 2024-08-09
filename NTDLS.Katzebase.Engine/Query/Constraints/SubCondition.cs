using NTDLS.Katzebase.Engine.Indexes.Matching;

namespace NTDLS.Katzebase.Engine.Query.Constraints
{
    internal class SubCondition
    {
        public bool IsRoot { get; set; } = false;
        public string Key { get; set; }
        public string Expression { get; set; }
        public List<Condition> Conditions { get; set; } = new();

        /// <summary>
        /// List of all expression keys referenced by the expression.
        /// </summary>
        public HashSet<string> ExpressionKeys { get; set; } = new();

        /// <summary>
        /// List of all condition keys referenced by the expression.
        /// </summary>
        public HashSet<string> ConditionKeys { get; set; } = new();

        /// <summary>
        /// If this condition is covered by an index, this is the index which we will use.
        /// </summary>
        public IndexSelection? IndexSelection { get; set; }

        public SubCondition(string key, string condition)
        {
            Key = key;
            Expression = condition;
        }
    }
}
