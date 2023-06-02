using Katzebase.Engine.Indexes;
using Katzebase.Engine.Query.Condition.NG;

namespace Katzebase.Engine.Query.Condition
{
    public class ConditionLookupOptimization
    {
        /// <summary>
        /// A list of the indexes that have been selected by the optimizer for the specified conditions.
        /// </summary>
        public List<IndexSelection> IndexSelection { get; set; } = new();

        /// <summary>
        /// A clone of the conditions that this set of index selections was built for. Also contains the indexes associated with each subset of conditions.
        /// </summary>
        public NGConditions Conditions { get; private set; }

        /// <summary>
        /// A flattened list of conditions, used to build the index selections (not used outside of index selection algorithms).
        /// </summary>
        //public List<FlatConditionGroup> FlatConditionGroups { get; private set; } = new();

        public ConditionLookupOptimization(NGConditions conditions)
        {
            Conditions = conditions.Clone();
        }
    }
}
