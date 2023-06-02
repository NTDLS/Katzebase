﻿using static Katzebase.Engine.Constants;

namespace Katzebase.Engine.Documents
{
    internal class DocumentLookupLogicSubsetResult
    {
        public LogicalConnector LogicalConnector { get; set; }
        public DocumentLookupResults Results { get; set; }
        public Guid SubsetUID { get; set; }

        public DocumentLookupLogicSubsetResult(Guid subsetUID, LogicalConnector logicalConnector, DocumentLookupResults results)
        {
            LogicalConnector = logicalConnector;
            SubsetUID = subsetUID;
            Results = results;
        }
    }
}
