using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Atomicity;
using NTDLS.Katzebase.Engine.Documents;
using NTDLS.Katzebase.Engine.Schemas;
using NTDLS.Katzebase.Parsers.Indexes.Matching;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Query.WhereAndJoinConditions;

using NTDLS.Katzebase.Parsers.Interfaces;
namespace NTDLS.Katzebase.Engine.Threading.PoolingParameters
{
    /// <summary>
    /// Thread parameters for a lookup operations. Shared across all threads in a single operation.
    /// </summary>
    class MatchSchemaDocumentsByConditionsOperation<TData> where TData : IStringable
    {
        public Dictionary<uint, DocumentPointer<TData>> ThreadResults = new();
        public PreparedQuery<TData> Query { get; set; }
        public Transaction<TData> Transaction { get; set; }
        public IndexingConditionLookup<TData> Lookup { get; set; }
        public PhysicalSchema<TData> PhysicalSchema { get; set; }
        public string WorkingSchemaPrefix { get; set; }
        public ConditionEntry<TData> Condition { get; set; }

        public KbInsensitiveDictionary<TData?>? KeyValues { get; set; }

        public MatchSchemaDocumentsByConditionsOperation(Transaction<TData> transaction, PreparedQuery<TData> query, IndexingConditionLookup<TData> lookup,
            PhysicalSchema<TData> physicalSchema, string workingSchemaPrefix, ConditionEntry<TData> condition, KbInsensitiveDictionary<TData?>? keyValues = null)
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
            public MatchSchemaDocumentsByConditionsOperation<TData> Operation { get; set; }
            public uint IndexPartition { get; set; }

            public Instance(MatchSchemaDocumentsByConditionsOperation<TData> operation, uint indexPartition)
            {
                Operation = operation;
                IndexPartition = indexPartition;
            }
        }
    }
}
