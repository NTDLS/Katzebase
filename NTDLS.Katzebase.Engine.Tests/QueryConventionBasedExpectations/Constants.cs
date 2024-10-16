namespace NTDLS.Katzebase.Engine.Tests.QueryConventionBasedExpectations
{
    internal class Constants
    {
        public enum DatasetExpectationOption
        {
            EnforceRowOrder,
            AffectedCount,
            MaxDuration,
            DoNotValidate
        }

        public enum BatchExpectationOption
        {
            DoNotValidate
        }

        public enum FieldPatternType
        {
            /// <summary>
            /// Exact match on this field. This is the default.
            /// </summary>
            Exact,
            /// <summary>
            // Simple pattern match: Use 'n'=any-number, 'c'=any--non-numeric-character, '_'=Any-alphanumeric
            // e.g. a date might be "2024/10/06 10:30 pm"-> "nnnn/nn/nn nn:nn cc"
            /// </summary>
            Format,
            /// <summary>
            /// Uses a LIKE match with '_' for any character, and '%' on the beginning and/or to match zero or more characters.
            /// </summary>
            Like,
            /// <summary>
            /// The field should contain any not-null value.
            /// </summary>
            NotNull,
            /// <summary>
            /// The field should contain a null value.
            /// </summary>
            Null,
            /// <summary>
            /// The value should be numeric.
            /// </summary>
            Numeric,
            /// The value should be an integer (similar to Numeric, but cannot contain decimal places).
            /// </summary>
            Integer,
            /// <summary>
            /// Value should contain a valid date/time.
            /// </summary>
            DateTime,
            /// <summary>
            /// Value should contain a valid Guid.
            /// </summary>
            Guid
        }
    }
}
