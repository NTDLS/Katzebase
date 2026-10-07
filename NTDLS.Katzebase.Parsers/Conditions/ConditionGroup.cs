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

        /// <summary>
        /// The index lookups selected for this group, ordered from most to least selective. Since all conditions
        /// in the group are ANDed, the documents matching the group are the intersection of the lookups.
        /// </summary>
        public List<IndexingConditionLookup> IndexLookups { get; set; } = new();

        /// <summary>
        /// The most selective index lookup for this group, if any.
        /// </summary>
        public IndexingConditionLookup? IndexLookup => IndexLookups.Count > 0 ? IndexLookups[0] : null;

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
