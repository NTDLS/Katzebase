namespace NTDLS.Katzebase.Shared
{
    public static class EngineConstants
    {
        /// <summary>
        /// File that contains the documents and an identity auto-incrementing value.
        /// </summary>
        public const string DocumentsFile = "@documents.kbrdb";
        /// <summary>
        /// File that contains the schema. Indexes, policies, procedures, etc.
        /// </summary>
        public const string SchemaFile = "@schema.kbrdb";

        public const string ProcedureCatalogFile = "@procedures.kbcat"; //TODO: Remove this, it will go in the SchemaFile.
        public const string TransactionAtomsFile = "@transaction.kbatom";
        public const string HealthStatsFile = "@health.kblog";
        public static readonly Guid RootSchemaGUID = Guid.Parse("0AABFAFA-5736-4BD9-BA74-E4998E137528");
        public const string UIDMarker = "$UID$";
        public const string PrimaryIdentityKey = "Primary";

        public enum KbColumnFamilyName
        {
            /// <summary>
            /// Column family name that contains document data.
            /// Every schema must have exactly one column family with this name.
            /// </summary>
            Documents,
            /// <summary>
            /// Column family name that contains auto-incrementing identity values,
            /// </summary>
            Identity,

            Schema,
            Indexes,
            Policy,
            Procedures,
        }

        public enum TraceType
        {
            CreateSchema,
            DocumentList,
            DocumentSample,
            DocumentStore,
            DoesSchemaExist,
            DropSchema,
            ExecuteExplainOperation,
            ExecuteExplainPlan,
            ExecuteStatementNonQuery,
            ExecuteStatementProcedure,
            ExecuteStatementQuery,
            IndexCreate,
            IndexDrop,
            IndexExist,
            IndexGet,
            IndexList,
            IndexRebuild,
            ListSchemas,
            SchemaFieldSample,
            SessionClose,
            SessionStart,
            SessionTerminate,
            TransactionBegin,
            TransactionCommit,
            TransactionRollback,
        }

        public enum TraceResult
        {
            Failure,
            Success
        }

        public enum StateSetting
        {
            TraceWaitTimes,
            WarnMissingFields,
            WarnNullPropagation,
            /// <summary>
            /// Causes the transaction to place Stability locks in place of Read locks.
            /// </summary>
            ReadUncommitted
        }

        public enum FieldCollapseType
        {
            ScalerSelect,
            AggregateSelect,
            ScalerOrderBy,
            AggregateOrderBy
        }

        public enum SecurityPolicyRule
        {
            Grant,
            Deny
        }

        public enum SecurityPolicyPermission
        {
            All,
            /// <summary>
            /// Select.
            /// </summary>
            Read,
            /// <summary>
            /// Update/Insert/Delete.
            /// </summary>
            Write,
            /// <summary>
            /// Drop/Create objects within the schema (such as sub-schemas, indexes, etc).
            /// </summary>
            Manage
        }

        public enum IndexMatchType
        {
            None,
            Full,
            Partial
        }

        public enum IOFormat
        {
            Raw,
            JSON,
            PBuf
        }

        public enum ActionType
        {
            KeyCreate,
            KeyAlter,
            KeyDelete,
            /// <summary>
            /// Create a column family.
            /// </summary>
            CfCreate
        }

        public enum HealthCounterType
        {
            IOCacheReadHits,
            IODeferredReads,
            IODeferredWrites,
            IOCacheReadMisses,
            IOCacheReadAdditions,
            IOCacheWriteAdditions,
            LockWaitMs,
            DeadlockCount,
            Warnings,
            Exceptions
        }

        public enum LockGranularity
        {
            /// <summary>
            /// All files in a path.
            /// </summary>
            Path = 1,
            /// <summary>
            /// A single file.
            /// </summary>
            Object = 2,
            /// <summary>
            /// All files in a path and all paths below it.
            /// </summary>
            PathRecursive = 3
        }

        public enum LockOperation
        {
            /// <summary>
            /// Do not allow deletes.
            /// </summary>
            Stability,
            /// <summary>
            /// Do not allow writes or deletes.
            /// </summary>
            Read,
            /// <summary>
            /// Do not allow other reads, writes or deletes.
            /// </summary>
            Write,
            /// <summary>
            /// Do not allow read, write, delete or stability.
            /// </summary>
            Delete
        }
    }
}
