using static Katzebase.Engine.Constants;

namespace Katzebase.Engine.Query
{
    public class ConditionSubset : ConditionBase
    {
        public LogicalConnector LogicalConnector { get; set; } = LogicalConnector.None;
        public List<ConditionGroup> Groups { get; set; } = new();

        public ConditionSubset(LogicalConnector logicalConnector)
        {
            LogicalConnector = logicalConnector;
        }

    }
}
