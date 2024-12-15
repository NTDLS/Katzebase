﻿using NTDLS.Katzebase.Api.Payloads;
using NTDLS.Katzebase.Engine.Interactions.Management;
using NTDLS.Katzebase.Parsers;
using NTDLS.ReliableMessaging;
using System.Diagnostics;
using static NTDLS.Katzebase.Shared.EngineConstants;

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
            using var trace = _core.Trace.CreateTracker(TraceType.ExecuteExplainPlan, context.ConnectionId);
            var session = _core.Sessions.GetSession(context.ConnectionId);
            trace.SetSession(session);

#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            LogManager.Debug(Thread.CurrentThread.Name);
#endif

            try
            {
                var apiResults = new KbQueryQueryExplainPlanReply();

                session.PushCurrentQuery(param.Statement);

                var queries = StaticBatchParser.Parse(param.Statement, _core.GlobalConstants, param.UserParameters);
                foreach (var query in queries)
                {
                    var apiResult = _core.Query.ExplainPlan(session, query);
                    apiResults.Add(apiResult);
                }

                session.PopCurrentQuery();

                return apiResults;
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        public KbQueryQueryExplainOperationReply ExecuteExplainOperation(RmContext context, KbQueryQueryExplainOperation param)
        {
            using var trace = _core.Trace.CreateTracker(TraceType.ExecuteExplainOperation, context.ConnectionId);
            var session = _core.Sessions.GetSession(context.ConnectionId);
            trace.SetSession(session);

#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            LogManager.Debug(Thread.CurrentThread.Name);
#endif
            try
            {
                var apiResults = new KbQueryQueryExplainOperationReply();

                session.PushCurrentQuery(param.Statement);

                var queries = StaticBatchParser.Parse(param.Statement, _core.GlobalConstants, param.UserParameters);
                foreach (var query in queries)
                {
                    var apiResult = _core.Query.ExplainOperations(session, query);
                    apiResults.Add(apiResult);
                }

                session.PopCurrentQuery();

                return apiResults;
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        public KbQueryProcedureExecuteReply ExecuteStatementProcedure(RmContext context, KbQueryProcedureExecute param)
        {
            using var trace = _core.Trace.CreateTracker(TraceType.ExecuteStatementProcedure, context.ConnectionId);
            var session = _core.Sessions.GetSession(context.ConnectionId);
            trace.SetSession(session);

#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            LogManager.Debug(Thread.CurrentThread.Name);
#endif
            try
            {
                session.PushCurrentQuery(param.Procedure.ProcedureName);
                var apiResults = (KbQueryProcedureExecuteReply)_core.Query.ExecuteProcedure(session, param.Procedure);
                session.PopCurrentQuery();

                return apiResults;
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        public KbQueryQueryExecuteQueryReply ExecuteStatementQuery(RmContext context, KbQueryQueryExecuteQuery param)
        {
            using var trace = _core.Trace.CreateTracker(TraceType.ExecuteStatementQuery, context.ConnectionId);
            var session = _core.Sessions.GetSession(context.ConnectionId);
            trace.SetSession(session);

#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            LogManager.Debug(Thread.CurrentThread.Name);
#endif
            try
            {
                var apiResults = new KbQueryQueryExecuteQueryReply();

                session.PushCurrentQuery(param.Statement);

                var queries = StaticBatchParser.Parse(param.Statement, _core.GlobalConstants, param.UserParameters);
                foreach (var query in queries)
                {
                    var apiResult = _core.Query.ExecuteQuery(session, query);
                    apiResults.Add(apiResult);
                }

                session.PopCurrentQuery();

                return apiResults;
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        public KbQueryQueryExecuteNonQueryReply ExecuteStatementNonQuery(RmContext context, KbQueryQueryExecuteNonQuery param)
        {
            using var trace = _core.Trace.CreateTracker(TraceType.ExecuteStatementNonQuery, context.ConnectionId);
            var session = _core.Sessions.GetSession(context.ConnectionId);
            trace.SetSession(session);

#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            LogManager.Debug(Thread.CurrentThread.Name);
#endif
            try
            {
                var apiResults = new KbQueryQueryExecuteNonQueryReply();

                session.PushCurrentQuery(param.Statement);

                var queries = StaticBatchParser.Parse(param.Statement, _core.GlobalConstants, param.UserParameters);
                foreach (var query in queries)
                {
                    var apiResult = _core.Query.ExecuteNonQuery(session, query);
                    apiResults.Add(apiResult);
                }

                session.PopCurrentQuery();

                return apiResults;
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }
    }
}
