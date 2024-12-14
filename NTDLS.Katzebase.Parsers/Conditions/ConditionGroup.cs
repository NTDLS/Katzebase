using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Conditions
{
    /// <summary>
    /// A collection of conditions of which all are to be evaluated with AND connectors.
    /// </summary>
    public class ConditionGroup : ICondition
    {
        public LogicalConnector LogicalConnector { get; set; }

        public HashSet<IndexSelection> UsableIndexes { get; set; } = new();

        public IndexingConditionLookup? IndexLookup { get; set; }

        public List<ICondition> Collection { get; set; } = new();

        public ConditionGroup(LogicalConnector logicalConnector)
        {
            LogicalConnector = logicalConnector;
        }

        public ICondition Clone()
        {
            var clone = new ConditionGroup(LogicalConnector);

            foreach (var entry in Collection)
            {
                clone.Collection.Add(entry.Clone());
            }

            foreach (var usableIndex in UsableIndexes)
            {
                clone.UsableIndexes.Add(usableIndex.Clone());
            }

            return clone;
        }
    }
}
