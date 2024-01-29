using NTDLS.Katzebase.Client;
using NTDLS.Katzebase.Client.Payloads.Queries;
using NTDLS.Katzebase.Engine;

namespace NTDLS.Katzebase.Service.APIHandlers
{
    public class QueryController
    {
        private readonly EngineCore _core;
        public QueryController(EngineCore core)
        {
            _core = core;
        }

        public KbQueryQueryExplainReply ExplainQuery(KbQueryQueryExplain param)
        {
            try
            {
                var processId = _core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                _core.Log.Trace(Thread.CurrentThread.Name);

                return _core.Query.APIHandlers.ExecuteStatementExplain(processId, param.Statement);
            }
            catch (Exception ex)
            {
                return new KbQueryQueryExplainReply
                {
                    ExceptionText = ex.Message,
                    Success = false
                };
            }
        }

        public KbQueryQueryExecuteQueryReply ExecuteQuery(KbQueryQueryExecuteQuery param)
        {
            try
            {
                var processId = _core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                _core.Log.Trace(Thread.CurrentThread.Name);

                return _core.Query.APIHandlers.ExecuteStatementQuery(processId, param.Statement);
            }
            catch (Exception ex)
            {
                return new KbQueryQueryExecuteQueryReply
                {
                    ExceptionText = ex.Message,
                    Success = false
                };
            }
        }

        public KbQueryQueryExecuteQueriesReply ExecuteQueries(KbQueryQueryExecuteQueries param)
        {
            try
            {
                var processId = _core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                _core.Log.Trace(Thread.CurrentThread.Name);

                return _core.Query.APIHandlers.ExecuteStatementQueries(processId, param.Statements);
            }
            catch (Exception ex)
            {
                return new KbQueryQueryExecuteQueriesReply
                {
                    ExceptionText = ex.Message,
                    Success = false
                };
            }
        }

        public KbQueryQueryExecuteNonQueryReply ExecuteNonQuery(KbQueryQueryExecuteNonQuery param)
        {
            try
            {
                var processId = _core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                _core.Log.Trace(Thread.CurrentThread.Name);

                return _core.Query.APIHandlers.ExecuteStatementNonQuery(processId, param.Statement);
            }
            catch (Exception ex)
            {
                return new KbQueryQueryExecuteNonQueryReply
                {
                    ExceptionText = ex.Message,
                    Success = false
                };
            }
        }
    }
}
