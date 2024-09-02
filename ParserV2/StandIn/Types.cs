namespace ParserV2.StandIn
{
    /// <summary>
    /// Types that are dummied in from the engine. We will drop these and use the ones from the engine once we integrate the parser.
    /// </summary>
    internal class Types
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

        internal enum KbScalerFunctionParameterType
        {
            Undefined,
            String,
            Boolean,
            Numeric,
            Infinite_String,
            optional_string
        }
    }
}
