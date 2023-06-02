using Katzebase.Engine.Indexes;
using static Katzebase.Engine.Constants;

namespace Katzebase.Engine.Query.Condition
{
    public class ConditionGroup : ConditionSubset
    {
        public ConditionGroup(LogicalConnector logicalConnector)
            : base(logicalConnector)
        {
        }
    }
}
