using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.WhereAndJoinConditions
{
    /// <summary>
    /// A collection of conditions of which all are to be evaluated with AND connectors.
    /// </summary>
    internal class Old_ConditionSetCollection : List<Old_ConditionSet>
    {
        public LogicalConnector Connector { get; set; }

        public Old_ConditionSetCollection(LogicalConnector logicalConnector)
        {
            Connector = logicalConnector;
        }

        public Old_ConditionSetCollection Clone()
        {
            var clone = new Old_ConditionSetCollection(Connector);

            foreach (var condition in this)
            {
                clone.Add(condition.Clone());
            }

            return clone;
        }
    }
}
