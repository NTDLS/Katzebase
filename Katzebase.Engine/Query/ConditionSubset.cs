using static Katzebase.Engine.Constants;

namespace Katzebase.Engine.Query
{
    public class ConditionSubset : ConditionBase
    {
        public LogicalConnector LogicalConnector { get; set; } = LogicalConnector.None;
        public Conditions Children { get; set; } = new();
    }
}
