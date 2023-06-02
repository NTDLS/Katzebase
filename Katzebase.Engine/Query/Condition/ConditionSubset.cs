using Katzebase.Engine.Indexes;
using static Katzebase.Engine.Constants;

namespace Katzebase.Engine.Query.Condition
{
    public class ConditionSubset : ICondition
    {
        public List<ICondition> Conditions = new();
        public LogicalConnector LogicalConnector { get; set; }
        public Guid UID { get; private set; } = Guid.NewGuid();
        public string SubsetVariableName { get; set; } = string.Empty;

        /// <summary>
        /// If this condition is covered by an index, this is the index which we will use.
        /// </summary>
        public IndexSelection? Index { get; set; }

        public ConditionSubset(LogicalConnector logicalConnector)
        {
            LogicalConnector = logicalConnector;
        }

        public ConditionSubset(LogicalConnector logicalConnector, Guid uid)
        {
            LogicalConnector = logicalConnector;
            UID = uid;
        }

        public ICondition Clone()
        {
            var clone = new ConditionSubset(LogicalConnector)
            {
                Index = Index,
                UID = UID,
                SubsetVariableName = SubsetVariableName
            };

            foreach (var condition in Conditions)
            {
                clone.Conditions.Add(condition.Clone());
            }

            return clone;
        }
    }
}
