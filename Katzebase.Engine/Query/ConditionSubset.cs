using static Katzebase.Engine.Constants;

namespace Katzebase.Engine.Query
{
    public class ConditionSubset : ConditionBase
    {
        public List<ConditionBase> Conditions = new();

        public LogicalConnector LogicalConnector { get; set; }

        public ConditionSubset(LogicalConnector logicalConnector)
        {
            LogicalConnector = logicalConnector;                
        }
    }
}
