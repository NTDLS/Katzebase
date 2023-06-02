using Katzebase.Engine.Indexes;
using static Katzebase.Engine.Constants;

namespace Katzebase.Engine.Query.Condition.NG
{
    public class NGCondition
    {
        public bool CoveredByIndex { get; set; } = false;
        public string SubsetKey { get; set; }
        public string ConditionKey { get; set; }
        public ConditionValue Left { get; set; } = new();
        public ConditionValue Right { get; set; } = new();
        public LogicalConnector LogicalConnector { get; set; } = LogicalConnector.None;
        public LogicalQualifier LogicalQualifier { get; set; } = LogicalQualifier.None;

        public NGCondition(string subsetKey, string conditionKey, LogicalConnector logicalConnector, string left, LogicalQualifier logicalQualifier, string right)
        {
            SubsetKey = subsetKey;
            ConditionKey = conditionKey;
            Left.Value = left;
            Right.Value = right;
            LogicalConnector = logicalConnector;
            LogicalQualifier = logicalQualifier;
        }

        public NGCondition Clone()
        {
            var clone = new NGCondition(SubsetKey, ConditionKey, LogicalConnector, Left.Value ?? string.Empty, LogicalQualifier, Right.Value ?? string.Empty);

            return clone;
        }
    }
}
