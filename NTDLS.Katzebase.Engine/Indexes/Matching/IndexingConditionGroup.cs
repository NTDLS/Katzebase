using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Indexes.Matching
{
    /// <summary>
    /// Contains a list of nested operations that will be used for indexing operations.
    /// </summary>
    internal class IndexingConditionGroup
    {
        public LogicalConnector LogicalConnector { get; set; } = LogicalConnector.None;

        /// <summary>
        /// These are the conditions that we need to process first.
        /// </summary>
        public List<IndexingConditionLookup> Lookups { get; set; } = new();

        /// <summary>
        /// If there are any sub-indexing-operations, then we need to process them after Conditions and then
        ///     either merge or intersect depending on the LogicalConnector of the sub-indexing-operations.
        /// </summary>
        public List<IndexingConditionGroup> SubIndexingConditionGroups { get; set; } = new();

        public IndexingConditionGroup(LogicalConnector logicalConnector)
        {
            LogicalConnector = logicalConnector;
        }
    }
}
