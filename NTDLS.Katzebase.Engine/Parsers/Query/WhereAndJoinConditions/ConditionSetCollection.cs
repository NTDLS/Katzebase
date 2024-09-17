using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.WhereAndJoinConditions
{
    /// <summary>
    /// A collection of conditions of which all are to be evaluated with AND connectors.
    /// </summary>
    internal class ConditionSetCollection : List<ConditionGroup>
    {
        public LogicalConnector Connector { get; set; }

        public ConditionSetCollection(LogicalConnector logicalConnector)
        {
            Connector = logicalConnector;
        }
    }
}
