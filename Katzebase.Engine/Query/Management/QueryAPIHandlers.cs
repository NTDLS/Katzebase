using Katzebase.PublicLibrary.Exceptions;
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

        public KbQueryResultCollection ExecuteStatementExplain(ulong processId, string statement)
        {
            var results = new KbQueryResultCollection();
            foreach (var preparedQuery in StaticQueryParser.PrepareBatch(statement))
            {
                results.Add(core.Query.ExplainQuery(processId, preparedQuery));
            }
            return results;
        }

        public KbQueryResultCollection ExecuteStatementProcedure(ulong processId, KbProcedure procedure)
        {
            return core.Query.ExecureProcedure(processId, procedure);
        }

        public KbQueryResultCollection ExecuteStatementQuery(ulong processId, string statement)
        {
            var results = new KbQueryResultCollection();
            foreach (var preparedQuery in StaticQueryParser.PrepareBatch(statement))
            {
                results.Add(core.Query.ExecuteQuery(processId, preparedQuery));
            }
            return results;
        }

        public KbActionResponseCollection ExecuteStatementNonQuery(ulong processId, string statement)
        {
            var results = new KbActionResponseCollection();
            foreach (var preparedQuery in StaticQueryParser.PrepareBatch(statement))
            {
                results.Add(core.Query.ExecuteNonQuery(processId, preparedQuery));
            }
            return results;
        }
    }
}
