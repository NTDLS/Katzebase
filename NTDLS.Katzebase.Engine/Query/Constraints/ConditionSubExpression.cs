using NTDLS.Katzebase.Engine.Indexes.Matching;

namespace NTDLS.Katzebase.Engine.Query.Constraints
{
    internal class ConditionSubExpression
    {
        public bool IsRoot { get; set; } = false;
        public string SubExpressionKey { get; set; }
        public string Expression { get; set; }
        public List<ConditionExpression> Expressions { get; set; } = new();
        public HashSet<string> SubExpressionKeys { get; set; } = new();
        public HashSet<string> ConditionKeys { get; set; } = new();

        /// <summary>
        /// If this condition is covered by an index, this is the index which we will use.
        /// </summary>
        public IndexSelection? IndexSelection { get; set; }

        public ConditionSubExpression(string subExpressionKey, string expression)
        {
            SubExpressionKey = subExpressionKey;
            Expression = expression;
        }
    }
}
