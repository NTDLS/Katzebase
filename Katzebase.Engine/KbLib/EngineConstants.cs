namespace Katzebase.Engine.KbLib
{
    public static class EngineConstants
    {
        public const string DocumentPageExtension = ".kbpage";
        public const string SchemaCatalogFile = "schemas.kbcat";
        public const string DocumentPageCatalogFile = "pages.kbcat";
        public const string IndexCatalogFile = "indexes.kbcat";
        public const string LoginCatalogFile = "logins.kbcat";
        public const string TransactionActionsFile = "transaction.kblog";
        public const string HealthStatsFile = "health.json";
        public static Guid RootSchemaGUID = Guid.Parse("0AABFAFA-5736-4BD9-BA74-E4998E137528");

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
            Create,
            Delete,
            Begin,
            Rollback,
            Commit,
            Drop,
            Rebuild,
            Set
        }

        public enum SubQueryType
        {
            None,
            Schemas,
            Documents,
            Transaction,
            Index,
            UniqueKey
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
