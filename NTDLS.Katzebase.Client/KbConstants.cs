namespace NTDLS.Katzebase.Client
{
    public static class KbConstants
    {
        public static string FriendlyName = "Katzebase";

        public enum KbLogSeverity
        {
            Trace = 0, //Super-verbose, debug-like information.
            Verbose = 1, //General status messages.
            Warning = 2, //Something the user might want to be aware of.
            Exception = 3 //An actual exception has been thrown.
        }

        public enum KbTransactionWarning
        {
            ResultDisqualifiedByNullValue,
            SelectFieldNotFound,
            SortFieldNotFound,
            MethodFieldNotFound,
            GroupFieldNotFound,
            ConditionFieldNotFound
        }

        public enum KbMessageType
        {
            Verbose,
            Warning,
            Error,
            Deadlock
        }

        public enum KbSortDirection
        {
            Ascending,
            Descending
        }

        public enum KbMetricType
        {
            Descrete,
            Cumulative
        }
    }
}
