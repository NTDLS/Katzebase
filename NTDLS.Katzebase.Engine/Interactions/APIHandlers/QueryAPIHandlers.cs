using NTDLS.Katzebase.Client.Payloads.RoundTrip;
using NTDLS.Katzebase.Engine.Query;
using NTDLS.ReliableMessaging;

namespace NTDLS.Katzebase.Engine.Interactions.APIHandlers
{
    /// <summary>
    /// Public class methods for handling API requests related to queries.
    /// </summary>
    public class QueryAPIHandlers : IRmMessageHandler
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

        public KbQueryQueryExplainReply ExecuteStatementExplain(RmContext context, KbQueryQueryExplain param)
        {
            var processId = _core.Sessions.UpsertConnectionId(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{processId}:{param.GetType().Name}";
            _core.Log.Trace(Thread.CurrentThread.Name);
#endif
            var results = new KbQueryQueryExplainReply();
            foreach (var preparedQuery in StaticQueryParser.PrepareBatch(param.Statement))
            {
                results.Add(_core.Query.ExplainQuery(processId, preparedQuery));
            }
            return results;
        }

        public KbQueryProcedureExecuteReply ExecuteStatementProcedure(RmContext context, KbQueryProcedureExecute param)
        {
            var processId = _core.Sessions.UpsertConnectionId(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{processId}:{param.GetType().Name}";
            _core.Log.Trace(Thread.CurrentThread.Name);
#endif
            return (KbQueryProcedureExecuteReply)_core.Query.ExecuteProcedure(processId, param.Procedure);
        }

        public KbQueryQueryExecuteQueryReply ExecuteStatementQuery(RmContext context, KbQueryQueryExecuteQuery param)
        {
            var processId = _core.Sessions.UpsertConnectionId(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{processId}:{param.GetType().Name}";
            _core.Log.Trace(Thread.CurrentThread.Name);
#endif
            var results = new KbQueryQueryExecuteQueryReply();
            foreach (var preparedQuery in StaticQueryParser.PrepareBatch(param.Statement))
            {
                results.Add(_core.Query.ExecuteQuery(processId, preparedQuery));
            }
            return results;
        }

        public KbQueryQueryExecuteQueriesReply ExecuteStatementQueries(RmContext context, KbQueryQueryExecuteQueries param)
        {
            var processId = _core.Sessions.UpsertConnectionId(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{processId}:{param.GetType().Name}";
            _core.Log.Trace(Thread.CurrentThread.Name);
#endif
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

        public KbQueryQueryExecuteNonQueryReply ExecuteStatementNonQuery(RmContext context, KbQueryQueryExecuteNonQuery param)
        {
            var processId = _core.Sessions.UpsertConnectionId(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{processId}:{param.GetType().Name}";
            _core.Log.Trace(Thread.CurrentThread.Name);
#endif
            var results = new KbQueryQueryExecuteNonQueryReply();
            foreach (var preparedQuery in StaticQueryParser.PrepareBatch(param.Statement))
            {
                results.Add(_core.Query.ExecuteNonQuery(processId, preparedQuery));
            }
            return results;
        }
    }
}
