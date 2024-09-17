using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.WhereAndJoinConditions
{
    /// <summary>
    /// A collection of conditions of which all are to be evaluated with AND connectors.
    /// </summary>
    internal class ConditionGroup : ICondition
    {
        public LogicalConnector Connector { get; set; }

        public List<ICondition> Collection { get; set; } = new();

        public ConditionGroup(LogicalConnector logicalConnector)
        {
            Connector = logicalConnector;
        }
    }
}
