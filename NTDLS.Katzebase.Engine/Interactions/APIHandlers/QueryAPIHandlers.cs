using NTDLS.Katzebase.Api.Payloads;
using NTDLS.Katzebase.Engine.Interactions.Management;
using NTDLS.Katzebase.Parsers.Query;
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

            foreach (var query in StaticParserBatch.Parse(param.Statement, _core.GlobalConstants, param.UserParameters))
            {
                results.Add(_core.Query.ExplainPlan(session, query));
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

            foreach (var query in StaticParserBatch.Parse(param.Statement, _core.GlobalConstants, param.UserParameters))
            {
                results.Add(_core.Query.ExplainOperations(session, query));
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
            var results = new KbQueryQueryExecuteQueryReply();

            session.SetCurrentQuery(param.Statement);

            foreach (var query in StaticParserBatch.Parse(param.Statement, _core.GlobalConstants, param.UserParameters))
            {
                results.Add(_core.Query.ExecuteQuery(session, query));
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

            var results = new KbQueryQueryExecuteNonQueryReply();

            session.SetCurrentQuery(param.Statement);

            foreach (var query in StaticParserBatch.Parse(param.Statement, _core.GlobalConstants, param.UserParameters))
            {
                results.Add(_core.Query.ExecuteNonQuery(session, query));
            }

            session.ClearCurrentQuery();

            return results;
        }
    }
}
