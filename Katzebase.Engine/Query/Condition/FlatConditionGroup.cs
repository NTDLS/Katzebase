using Katzebase.Engine.Indexes;
using static Katzebase.Engine.Constants;

namespace Katzebase.Engine.Query.Condition
{
    public class FlatConditionGroup
    {
        public List<ConditionSingle> Conditions = new();

        public LogicalConnector LogicalConnector { get; set; }

        private Guid _subsetUID;
        public Guid SubsetUID
        {
            get
            {
                return _subsetUID;
            }
            set
            {
                _subsetUID = value;
                _subsetVariableName = $"n{value.ToString().ToLower().Replace("-", "")}";
            }
        }
        private string _subsetVariableName = string.Empty;
        public string SubsetVariableName => _subsetVariableName;

        /// <summary>
        /// If this condition is covered by an index, this is the index which we will use.
        /// </summary>
        public IndexSelection? Index { get; set; }

        public ConditionSubset ToSubset()
        {
            var subset = new ConditionSubset(LogicalConnector, SubsetUID);

            foreach (var condition in Conditions)
            {
                subset.Conditions.Add(condition);
            }

            return subset;
        }

        public FlatConditionGroup(ConditionSubset subset)
        {
            SubsetUID = subset.SubsetUID;
            LogicalConnector = subset.LogicalConnector;
            Index = subset.Index;
        }
    }
}
