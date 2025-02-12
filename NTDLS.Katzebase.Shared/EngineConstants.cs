﻿namespace NTDLS.Katzebase.Shared
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
            WarnNullPropagation
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
            Read, //Select.
            Write, //Update/Insert/Delete.
            Manage //Drop/Create objects within the schema (such as sub-schemas, indexes, etc).
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
            Directory = 1,         //All files in a directory.
            File = 2,              //A single file.
            RecursiveDirectory = 3 //All files in a directory and all directories below it.
        }

        public enum LockOperation
        {
            Stability, //Do not allow deletes.
            Read,    //Do not allow writes or deletes.
            Write,   //Do not allow other reads, writes or deletes.
            Delete   //Do not allow read, write, delete or observe.
        }
    }
}
