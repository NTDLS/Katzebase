using NTDLS.Katzebase.Engine.Indexes.Matching;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

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

        public LogicalConnector LogicalConnector { get; private set; } = LogicalConnector.None;

        /// <summary>
        /// A selection of indexes which can be used to satisfy the Conditions.
        /// </summary>
        public HashSet<IndexSelection> IndexSelections { get; set; } = new();

        public SubCondition(string key, LogicalConnector logicalConnector, string condition)
        {
            LogicalConnector = logicalConnector;
            Key = key;
            Expression = condition;
        }
    }
}
