using static Katzebase.Engine.Constants;

namespace Katzebase.Engine.Query
{
    public class ConditionGroup
    {
        public List<ConditionBase> Conditions = new();

        public LogicalConnector LogicalConnector { get; set; }

        public ConditionGroup(LogicalConnector logicalConnector)
        {
            LogicalConnector = logicalConnector;
        }
    }
}
