using Katzebase.PublicLibrary.Payloads;

namespace Katzebase.Engine.Query.Management
{
    /// <summary>
    /// Public class methods for handling API requests related to queries.
    /// </summary>
    public class QueryAPIHandlers
    {
        private readonly Core core;

        public QueryAPIHandlers(Core core)
        {
            this.core = core;
        }

        public KbQueryResult ExecuteStatementExplain(ulong processId, string statement)
        {
            var preparedQuery = ParserEngine.ParseQuery(statement);
            return core.Query.ExplainQuery(processId, preparedQuery);
        }

        public KbQueryResult ExecuteStatementQuery(ulong processId, string statement)
        {
            var preparedQuery = ParserEngine.ParseQuery(statement);
            return core.Query.ExecuteQuery(processId, preparedQuery);
        }

        public KbActionResponse ExecuteStatementNonQuery(ulong processId, string statement)
        {
            var preparedQuery = ParserEngine.ParseQuery(statement);
            return core.Query.ExecuteNonQuery(processId, preparedQuery);
        }
    }
}
