using Katzebase.Engine.Indexes;
using static Katzebase.Engine.Constants;

namespace Katzebase.Engine.Query.Condition
{
    public class ConditionSubset : ICondition
    {
        public List<ICondition> Conditions = new();
        public LogicalConnector LogicalConnector { get; set; }

        public Guid SubsetUID { get; private set; }
        public string SubsetVariableName { get; set; } = string.Empty;

        /// <summary>
        /// If this condition is covered by an index, this is the index which we will use.
        /// </summary>
        public IndexSelection? IndexSelection { get; set; }

        public ConditionSubset(LogicalConnector logicalConnector)
        {
            LogicalConnector = logicalConnector;
            SubsetUID = Guid.NewGuid();
        }

        public ConditionSubset(LogicalConnector logicalConnector, Guid subsetUID)
        {
            LogicalConnector = logicalConnector;
            SubsetUID = subsetUID;
        }

        public List<ConditionSingle> Singles()
        {
            return Conditions.OfType<ConditionSingle>().ToList();
        }

        public List<ConditionSubset> Subsets()
        {
            return Conditions.OfType<ConditionSubset>().ToList();
        }

        public ICondition Clone()
        {
            var clone = new ConditionSubset(LogicalConnector)
            {
                IndexSelection = IndexSelection,
                SubsetUID = SubsetUID
            };

            foreach (var condition in Conditions)
            {
                clone.Conditions.Add(condition.Clone());
            }

            return clone;
        }
    }
}
