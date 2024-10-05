using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Schemas;
using NTDLS.Katzebase.Parsers.Indexes.Matching;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Query.WhereAndJoinConditions;
using NTDLS.Katzebase.PersistentTypes.Document;

namespace NTDLS.Katzebase.Engine.Threading.PoolingParameters
{
    /// <summary>
    /// Thread parameters for a lookup operations. Shared across all threads in a single operation.
    /// </summary>
    internal class MatchSchemaDocumentsByConditionsOperation
    {
        public Dictionary<uint, DocumentPointer> ThreadResults = new();
        public PreparedQuery Query { get; set; }
        public Transaction Transaction { get; set; }
        public IndexingConditionLookup Lookup { get; set; }
        public PhysicalSchema PhysicalSchema { get; set; }
        public string WorkingSchemaPrefix { get; set; }
        public ConditionEntry Condition { get; set; }

        public KbInsensitiveDictionary<string?>? KeyValues { get; set; }

        public MatchSchemaDocumentsByConditionsOperation(Transaction transaction, PreparedQuery query, IndexingConditionLookup lookup,
            PhysicalSchema physicalSchema, string workingSchemaPrefix, ConditionEntry condition, KbInsensitiveDictionary<string?>? keyValues = null)
        {
            Transaction = transaction;
            Query = query;
            Lookup = lookup;
            PhysicalSchema = physicalSchema;
            WorkingSchemaPrefix = workingSchemaPrefix;
            Condition = condition;
            KeyValues = keyValues;
        }

        /// <summary>
        /// Thread parameters for a lookup operations. Used by a single thread.
        /// </summary>
        public class Instance
        {
            public MatchSchemaDocumentsByConditionsOperation Operation { get; set; }
            public uint IndexPartition { get; set; }

            public Instance(MatchSchemaDocumentsByConditionsOperation operation, uint indexPartition)
            {
                Operation = operation;
                IndexPartition = indexPartition;
            }
        }
    }
}
