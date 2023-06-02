using static Katzebase.Engine.Constants;

namespace Katzebase.Engine.Documents
{
    internal class DocumentLookupLogicSubsetResult
    {
        public LogicalConnector LogicalConnector { get; set; }
        public DocumentLookupResults Results { get; set; }
        public Guid ConditionSubsetUID { get; set; }

        public DocumentLookupLogicSubsetResult(Guid conditionSubsetUID, LogicalConnector logicalConnector, DocumentLookupResults results)
        {
            LogicalConnector = logicalConnector;
            ConditionSubsetUID = conditionSubsetUID;
            Results = results;
        }
    }
}
