using NTDLS.Katzebase.Engine.Indexes.Matching;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.WhereAndJoinConditions
{
    /// <summary>
    /// A collection of conditions of which all are to be evaluated with AND connectors.
    /// </summary>
    internal class ConditionSet : List<Condition>
    {
        public LogicalConnector Connector { get; set; }

        /// <summary>
        /// A selection of indexes which can be used to satisfy the Conditions.
        /// </summary>
        public HashSet<IndexSelection> IndexSelections { get; set; } = new();

        public ConditionSet(LogicalConnector logicalConnector)
        {
            Connector = logicalConnector;
        }
    }
}
