using NTDLS.Katzebase.Engine.Indexes.Matching;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.WhereAndJoinConditions
{
    /// <summary>
    /// A collection of conditions of which all are to be evaluated with AND connectors.
    /// </summary>
    internal class Old_ConditionSet : List<Old_Condition>
    {
        public LogicalConnector Connector { get; set; }

        /// <summary>
        /// A selection of indexes which can be used to satisfy the Conditions.
        /// </summary>
        public HashSet<IndexSelection> UsableIndexes { get; set; } = new();

        public Old_ConditionSet(LogicalConnector logicalConnector)
        {
            Connector = logicalConnector;
        }

        public Old_ConditionSet Clone()
        {
            var clone = new Old_ConditionSet(Connector);

            foreach (var condition in this)
            {
                clone.Add(condition.Clone());
            }

            return clone;
        }
    }
}
