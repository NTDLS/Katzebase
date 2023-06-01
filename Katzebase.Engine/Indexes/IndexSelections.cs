using Katzebase.Engine.Query;

namespace Katzebase.Engine.Indexes
{
    public class IndexSelections : List<IndexSelection>
    {
        /// <summary>
        /// A clone of the conditions that this set of index selections was built for. Also contains the indexes associated with each subset of conditions.
        /// </summary>
        public Conditions Conditions { get; private set; }

        /// <summary>
        /// A flatened list of conditions, used to build the index selections (not used outside of index selection algorithms).
        /// </summary>
        public List<FlatConditionGroup> FlatConditionGroups { get; private set; }

        public IndexSelections(Conditions conditions)
        {
            Conditions = conditions.Clone();

            FlatConditionGroups = Conditions.Flatten();
            foreach (var flatConditionGroup in FlatConditionGroups)
            {
                //Order the conditions by None, And, Or - because this is the way the index selections will want them.
                flatConditionGroup.Conditions = flatConditionGroup.Conditions.OrderBy(o => o.LogicalConnector).ToList();
            }
        }
    }
}
