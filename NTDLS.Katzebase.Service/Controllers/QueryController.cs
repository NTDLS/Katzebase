using NTDLS.Katzebase.Client.Payloads.Queries;

namespace NTDLS.Katzebase.Client.Service.Controllers
{
    public static class QueryController
    {
        public static KbQueryQueryExplainReply ExplainQuery(KbQueryQueryExplain param)
        {
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                return Program.Core.Query.APIHandlers.ExecuteStatementExplain(processId, param.Statement);
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

        public static KbQueryQueryExecuteQueryReply ExecuteQuery(KbQueryQueryExecuteQuery param)
        {
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                return Program.Core.Query.APIHandlers.ExecuteStatementQuery(processId, param.Statement);
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

        public static KbQueryQueryExecuteQueriesReply ExecuteQueries(KbQueryQueryExecuteQueries param)
        {
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                return Program.Core.Query.APIHandlers.ExecuteStatementQueries(processId, param.Statements);
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

        public static KbQueryQueryExecuteNonQueryReply ExecuteNonQuery(KbQueryQueryExecuteNonQuery param)
        {
            try
            {
                var processId = Program.Core.Sessions.UpsertSessionId(param.SessionId);
                Thread.CurrentThread.Name = $"KbAPI:{processId}:{KbUtility.GetCurrentMethod()}";
                Program.Core.Log.Trace(Thread.CurrentThread.Name);

                return Program.Core.Query.APIHandlers.ExecuteStatementNonQuery(processId, param.Statement);
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
