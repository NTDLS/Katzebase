using NTDLS.Katzebase.Client.Payloads.RoundTrip;
using NTDLS.Katzebase.Engine.Query;

namespace NTDLS.Katzebase.Engine.Interactions.APIHandlers
{
    /// <summary>
    /// Public class methods for handling API requests related to queries.
    /// </summary>
    public class QueryAPIHandlers
    {
        private readonly EngineCore _core;

        public QueryAPIHandlers(EngineCore core)
        {
            _core = core;

            try
            {
            }
            catch (Exception ex)
            {
                core.Log.Write($"Failed to instantiate query API handlers.", ex);
                throw;
            }
        }

        public KbQueryQueryExplainReply ExecuteStatementExplain(ulong processId, KbQueryQueryExplain param)
        {
            var results = new KbQueryQueryExplainReply();
            foreach (var preparedQuery in StaticQueryParser.PrepareBatch(param.Statement))
            {
                results.Add(_core.Query.ExplainQuery(processId, preparedQuery));
            }
            return results;
        }

        public KbQueryProcedureExecuteReply ExecuteStatementProcedure(ulong processId, KbQueryProcedureExecute param)
        {
            return (KbQueryProcedureExecuteReply)_core.Query.ExecuteProcedure(processId, param.Procedure);
        }

        public KbQueryQueryExecuteQueryReply ExecuteStatementQuery(ulong processId, KbQueryQueryExecuteQuery param)
        {
            var results = new KbQueryQueryExecuteQueryReply();
            foreach (var preparedQuery in StaticQueryParser.PrepareBatch(param.Statement))
            {
                results.Add(_core.Query.ExecuteQuery(processId, preparedQuery));
            }
            return results;
        }

        public KbQueryQueryExecuteQueriesReply ExecuteStatementQueries(ulong processId, KbQueryQueryExecuteQueries param)
        {
            var results = new KbQueryQueryExecuteQueriesReply();

            foreach (var statement in param.Statements)
            {
                foreach (var preparedQuery in StaticQueryParser.PrepareBatch(statement))
                {
                    var intermediatResult = _core.Query.ExecuteQuery(processId, preparedQuery);

                    results.Add(intermediatResult);
                }
            }
            return results;
        }

        public KbQueryQueryExecuteNonQueryReply ExecuteStatementNonQuery(ulong processId, KbQueryQueryExecuteNonQuery param)
        {
            var results = new KbQueryQueryExecuteNonQueryReply();
            foreach (var preparedQuery in StaticQueryParser.PrepareBatch(param.Statement))
            {
                results.Add(_core.Query.ExecuteNonQuery(processId, preparedQuery));
            }
            return results;
        }
    }
}
