using Katzebase.Engine.Indexes;
using static Katzebase.Engine.Constants;

namespace Katzebase.Engine.Query.Condition
{
    public class FlatConditionGroup
    {
        public List<ConditionSingle> Conditions = new();

        public LogicalConnector LogicalConnector { get; set; }

        public Guid SourceSubsetUID { get; private set; }

        public string SubsetVariableName { get; set; }

        /// <summary>
        /// If this condition is covered by an index, this is the index which we will use.
        /// </summary>
        public IndexSelection? Index { get; set; }

        public ConditionSubset ToSubset()
        {
            var subset = new ConditionSubset(LogicalConnector, SourceSubsetUID);

            subset.SubsetVariableName = SubsetVariableName;

            foreach (var condition in Conditions)
            {
                subset.Conditions.Add(condition);
            }

            return subset;
        }

        public FlatConditionGroup(ConditionSubset subset)
        {
            SourceSubsetUID = subset.UID;
            LogicalConnector = subset.LogicalConnector;
            Index = subset.Index;
            SubsetVariableName = subset.SubsetVariableName;
        }
    }
}
