using NTDLS.Katzebase.Api.Payloads.RoundTrip;
using NTDLS.Katzebase.Engine.Interactions.Management;
using NTDLS.Katzebase.Parsers;
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
                LogManager.Error($"Failed to instantiate query API handlers.", ex);
                throw;
            }
        }

        public KbQueryQueryExplainPlanReply ExecuteExplainPlan(RmContext context, KbQueryQueryExplainPlan param)
        {
            var session = _core.Sessions.GetSession(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            LogManager.Debug(Thread.CurrentThread.Name);
#endif

            var results = new KbQueryQueryExplainPlanReply();

            session.SetCurrentQuery(param.Statement);

            foreach (var preparedQuery in StaticQueryParser.ParseBatch(param.Statement, _core.GlobalConstants, param.UserParameters))
            {
                var intermediateResult = _core.Query.ExplainPlan(session, preparedQuery);

                results.Add(intermediateResult);
            }

            session.ClearCurrentQuery();

            return results;
        }

        public KbQueryQueryExplainOperationReply ExecuteExplainOperation(RmContext context, KbQueryQueryExplainOperation param)
        {
            var session = _core.Sessions.GetSession(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            LogManager.Debug(Thread.CurrentThread.Name);
#endif

            var results = new KbQueryQueryExplainOperationReply();

            session.SetCurrentQuery(param.Statement);

            foreach (var preparedQuery in StaticQueryParser.ParseBatch(param.Statement, _core.GlobalConstants, param.UserParameters))
            {
                var intermediateResult = _core.Query.ExplainOperations(session, preparedQuery);

                results.Add(intermediateResult);
            }

            session.ClearCurrentQuery();

            return results;
        }

        public KbQueryProcedureExecuteReply ExecuteStatementProcedure(RmContext context, KbQueryProcedureExecute param)
        {
            var session = _core.Sessions.GetSession(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            LogManager.Debug(Thread.CurrentThread.Name);
#endif

            session.SetCurrentQuery(param.Procedure.ProcedureName);
            var result = (KbQueryProcedureExecuteReply)_core.Query.ExecuteProcedure(session, param.Procedure);
            session.ClearCurrentQuery();

            return result;
        }

        public KbQueryQueryExecuteQueryReply ExecuteStatementQuery(RmContext context, KbQueryQueryExecuteQuery param)
        {
            var session = _core.Sessions.GetSession(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            LogManager.Debug(Thread.CurrentThread.Name);
#endif
            session.SetCurrentQuery(param.Statement);

            var results = new KbQueryQueryExecuteQueryReply();
            foreach (var preparedQuery in StaticQueryParser.ParseBatch(param.Statement, _core.GlobalConstants, param.UserParameters))
            {
                results.Add(_core.Query.ExecuteQuery(session, preparedQuery));
            }

            session.ClearCurrentQuery();

            return results;
        }

        public KbQueryQueryExecuteNonQueryReply ExecuteStatementNonQuery(RmContext context, KbQueryQueryExecuteNonQuery param)
        {
            var session = _core.Sessions.GetSession(context.ConnectionId);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            LogManager.Debug(Thread.CurrentThread.Name);
#endif

            session.SetCurrentQuery(param.Statement);

            var results = new KbQueryQueryExecuteNonQueryReply();
            foreach (var preparedQuery in StaticQueryParser.ParseBatch(param.Statement, _core.GlobalConstants, param.UserParameters))
            {
                results.Add(_core.Query.ExecuteNonQuery(session, preparedQuery));
            }

            session.ClearCurrentQuery();

            return results;
        }
    }
}
