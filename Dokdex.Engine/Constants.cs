using System;

namespace Dokdex.Engine
{
    public static class Constants
    {
        public const string DocumentExtension = ".json";
        public const string SchemaCatalogFile = "@SchemaCatalog.json";
        public const string DocumentCatalogFile = "@DocumentCatalog.json";
        public const string IndexCatalogFile = "@IndexCatalog.json";
        public const string LoginCatalogFile = "@LoginCatalog.json";
        public const string TransactionActionsFile = "@Transaction.log";
        public const string HealthStatsFile = "@Health.json";
        public static Guid RootSchemaGUID = Guid.Parse("0AABFAFA-5736-4BD9-BA74-E4998E137528");
        public static int MaxSizeForInferredIODeferment = 1024;

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
            FileDelete,
            DirectoryCreate,
            DirectoryDelete
        }

        public enum HealthCounterType
        {
            IOCacheReadHits,
            IOCacheReadMisses,
            IOCacheReadAdditions,
            IOCacheWriteAdditions,
            LockWaitMs,
            DeadlockCount,
            Warnings,
            Exceptions
        }

        public enum LogSeverity
        {
            Trace = 0, //Super-verbose, debug-like information.
            Verbose = 1, //General status messages.
            Warning = 2, //Something the user might want to be aware of.
            Exception = 3 //An actual exception has been thrown.
        }

        public enum LockType
        {
            Directory = 1,
            File = 2
        }

        public enum LockOperation
        {
            Read,
            Write
        }

        public enum QueryType
        {
            Select,
            Insert,
            Update,
            Delete
        }

        public enum ConditionType
        {
            None,
            And,
            Or
        }

        public enum ConditionQualifier
        {
            None,
            Equals,
            Like,
            NotEquals,
            NotLike,
            LessThan,
            GreaterThan,
            LessThanOrEqual,
            GreaterThanOrEqual
        }
    }
}
