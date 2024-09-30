namespace NTDLS.Katzebase.Client
{
    public static class KbConstants
    {
        public static string FriendlyName = "Katzebase";

        public enum KbLogSeverity
        {
            /// <summary>
            /// The most detailed information. These messages may contain sensitive application data.
            /// They are typically only enabled during development.
            /// </summary>
            Verbose = 0,
            /// <summary>
            /// Diagnostic information useful for debugging.
            /// These messages are less detailed than Verbose but still more than what's typically needed during production.
            /// </summary>
            Debug = 1,
            /// <summary>
            /// Informational messages that highlight the progress of the application at a high level.
            /// These messages are useful for tracking the flow of the application.
            /// </summary>
            Information = 2,
            /// <summary>
            /// Potentially harmful situations that could lead to errors.
            /// These messages indicate a potential problem that should be investigated.
            /// </summary>
            Warning = 3,
            /// <summary>
            /// Errors that prevent the current operation from continuing.
            /// These messages indicate a failure in the current operation or request but not an application-wide failure.
            /// </summary>
            Error = 4,
            /// <summary>
            /// Very severe errors that lead to application termination.
            /// These messages indicate critical failures that require immediate attention.
            /// </summary>
            Fatal = 5
        }

        public enum KbBasicDataType
        {
            Undefined,
            String,
            Numeric
        }

        public enum KbTransactionWarning
        {
            ResultDisqualifiedByNullValue,
            AggregateDisqualifiedByNullValue,
            SelectFieldNotFound,
            SortFieldNotFound,
            MethodFieldNotFound,
            GroupFieldNotFound,
            ConditionFieldNotFound
        }

        public enum KbMessageType
        {
            /// <summary>
            /// When communicating verbose information with the query result.
            /// </summary>
            Verbose,
            /// <summary>
            /// When communicating warnings with the query result.
            /// </summary>
            Warning,
            /// <summary>
            /// When communicating errors with the query result.
            /// </summary>
            Error,
            /// <summary>
            /// Used when explaining index/query with the query result.
            /// </summary>
            Explain,
            /// <summary>
            /// When communicating deadlock messages with the query result.
            /// </summary>
            Deadlock
        }

        public enum KbSortDirection
        {
            Ascending,
            Descending
        }

        public enum KbMetricType
        {
            Discrete,
            Cumulative
        }
    }
}
