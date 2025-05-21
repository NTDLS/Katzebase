namespace NTDLS.Katzebase.Shared
{
    public static class EngineConstants
    {
        public const string IndexPageExtension = ".kbixpage";
        public const string DocumentPageExtension = ".kbpage";
        public const string DocumentPageDocumentIdExtension = ".kbmap";
        public const string SchemaCatalogFile = "@schemas.kbcat";
        public const string PolicyCatalogFile = "@policy.kbcat";
        public const string DocumentPageCatalogFile = "@pages.kbcat";
        public const string IndexCatalogFile = "@indexes.kbcat";
        public const string ProcedureCatalogFile = "@procedures.kbcat";
        public const string LoginCatalogFile = "@logins.kbcat";
        public const string TransactionActionsFile = "@transaction.kbatom";
        public const string HealthStatsFile = "@health.kblog";
        public static readonly Guid RootSchemaGUID = Guid.Parse("0AABFAFA-5736-4BD9-BA74-E4998E137528");
        public const string UIDMarker = "$UID$";

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
            FileCreate,
            FileAlter,
            FileRead,
            FileDelete,
            DirectoryCreate,
            DirectoryDelete
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
            /// All files in a directory.
            /// </summary>
            Directory = 1,
            /// <summary>
            /// A single file.
            /// </summary>
            File = 2,
            /// <summary>
            /// All files in a directory and all directories below it.
            /// </summary>
            RecursiveDirectory = 3
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
