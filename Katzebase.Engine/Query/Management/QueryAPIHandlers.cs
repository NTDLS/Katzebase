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

            try
            {
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to instanciate query API handlers.", ex);
                throw;
            }
        }

        public KbQueryResult ExecuteStatementExplain(ulong processId, string statement)
        {
            var preparedQuery = StaticQueryParser.PrepareQuery(statement);
            return core.Query.ExplainQuery(processId, preparedQuery);
        }

        public KbQueryResult ExecuteStatementQuery(ulong processId, string statement)
        {
            var preparedQuery = StaticQueryParser.PrepareQuery(statement);
            return core.Query.ExecuteQuery(processId, preparedQuery);
        }

        public KbActionResponse ExecuteStatementNonQuery(ulong processId, string statement)
        {
            var preparedQuery = StaticQueryParser.PrepareQuery(statement);
            return core.Query.ExecuteNonQuery(processId, preparedQuery);
        }
    }
}
