namespace NTDLS.Katzebase.Parsers
{
    public static class Constants
    {
        public enum QueryType
        {
            None,
            Alter,
            Analyze,
            Begin,
            Commit,
            Create,
            Delete,
            Drop,
            Exec,
            Insert,
            Kill,
            List,
            Rebuild,
            Rollback,
            Sample,
            Select,
            SelectInto,
            Set,
            Update
        }

        public enum SubQueryType
        {
            None,
            Account,
            Configuration,
            Documents,
            Index,
            Procedure,
            Role,
            Schema,
            Schemas,
            Transaction,
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
            Between,
            NotBetween,
            LessThan,
            GreaterThan,
            LessThanOrEqual,
            GreaterThanOrEqual
        }
    }
}
