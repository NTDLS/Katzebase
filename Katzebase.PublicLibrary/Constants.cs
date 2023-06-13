namespace Katzebase.PublicLibrary
{
    public static class Constants
    {
        public static string FriendlyName = "Katzebase";

        public enum LogSeverity
        {
            Trace = 0, //Super-verbose, debug-like information.
            Verbose = 1, //General status messages.
            Warning = 2, //Something the user might want to be aware of.
            Exception = 3 //An actual exception has been thrown.
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
