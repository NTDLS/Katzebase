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
            Declare,
            Delete,
            Deny,
            Drop,
            Exec,
            Grant,
            Insert,
            Kill,
            List,
            Rebuild,
            Revoke,
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
            AddUserToRole,
            Configuration,
            Documents,
            Index,
            Manage,
            Procedure,
            Read,
            RemoveUserFromRole,
            Role,
            Schema,
            Schemas,
            Transaction,
            UniqueKey,
            Write
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
            Between,
            Equals,
            GreaterThan,
            GreaterThanOrEqual,
            LessThan,
            LessThanOrEqual,
            Like,
            NotBetween,
            NotEquals,
            NotLike
        }
    }
}
