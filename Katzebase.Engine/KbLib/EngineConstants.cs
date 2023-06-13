namespace Katzebase.Engine.KbLib
{
    public static class EngineConstants
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
            None,
            Select,
            Sample,
            List,
            Alter,
            Insert,
            Update,
            Delete,
            Rebuild,
            Set
        }

        public enum SubQueryType
        {
            None,
            Schemas,
            Documents,
            Index
        }

        public enum LogicalConnector
        {
            None,
            And,
            Or
        }

        public enum LogicalQualifier
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
