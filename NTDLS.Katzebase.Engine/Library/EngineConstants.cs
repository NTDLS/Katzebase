namespace NTDLS.Katzebase.Engine.Library
{
    public static class EngineConstants
    {
        public static string IndexPageExtension { get; } = ".kbixpage";
        public static string DocumentPageExtension { get; } = ".kbpage";
        public static string DocumentPageDocumentIdExtension { get; } = ".kbmap";
        public static string SchemaCatalogFile { get; } = "@schemas.kbcat";
        public static string DocumentPageCatalogFile { get; } = "@pages.kbcat";
        public static string IndexCatalogFile { get; } = "@indexes.kbcat";
        public static string ProcedureCatalogFile { get; } = "@procedures.kbcat";
        public static string LoginCatalogFile { get; } = "@logins.kbcat";
        public static string TransactionActionsFile { get; } = "@transaction.kbatom";
        public static string HealthStatsFile { get; } = "@health.kblog";
        public static Guid RootSchemaGUID { get; } = Guid.Parse("0AABFAFA-5736-4BD9-BA74-E4998E137528");

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
            IODeferredIOReads,
            IODeferredIOWrites,
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
            Directory = 1, //All files in a directory.
            File = 2, //A single file.
            Path //All files in a directory and all directories below it.
        }

        public enum LockOperation
        {
            //SchemaPreserve, //Do not allow deletes.
            Read, //Do not allow writes or deletes.
            Write, //Do not allow other reads, writes or deletes.
            Delete //Do not allow read, preserve, write or delete.
        }

        public enum QueryType
        {
            None,
            Select,
            SelectInto,
            Sample,
            Analyze,
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
            Set,
            Kill,
            Exec
        }

        public enum SubQueryType
        {
            None,
            Schema,
            Schemas,
            Documents,
            Transaction,
            Configuration,
            Index,
            UniqueKey,
            Procedure
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
            Between,
            NotBetween,
            LessThan,
            GreaterThan,
            LessThanOrEqual,
            GreaterThanOrEqual
        }
    }
}
