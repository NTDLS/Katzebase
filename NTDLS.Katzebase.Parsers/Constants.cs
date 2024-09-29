namespace NTDLS.Katzebase.Parsers
{
    public static class Constants
    {
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
