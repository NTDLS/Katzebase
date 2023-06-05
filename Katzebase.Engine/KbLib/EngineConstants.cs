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
            Select,
            Insert,
            Update,
            Delete,
            Set
        }

        public enum LogicalConnector
        {
            None,
            And,
            Or
        }

        public static string LogicalConnectorToString(LogicalConnector logicalConnector)
        {
            return logicalConnector == LogicalConnector.None ? string.Empty : logicalConnector.ToString().ToUpper();
        }

        public static string LogicalConnectorToLogicString(LogicalConnector logicalConnector)
        {
            switch (logicalConnector)
            {
                case LogicalConnector.Or:
                    return "||";
                case LogicalConnector.And:
                    return "&&";
            }

            return string.Empty;
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

        public static string LogicalQualifierToString(LogicalQualifier logicalQualifier)
        {
            switch (logicalQualifier)
            {
                case LogicalQualifier.Equals:
                    return "=";
                case LogicalQualifier.NotEquals:
                    return "!=";
                case LogicalQualifier.GreaterThanOrEqual:
                    return ">=";
                case LogicalQualifier.LessThanOrEqual:
                    return "<=";
                case LogicalQualifier.LessThan:
                    return "<";
                case LogicalQualifier.GreaterThan:
                    return ">";
            }

            return "";
        }
    }
}
