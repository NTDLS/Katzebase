using NTDLS.Katzebase.Client.Payloads;
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

        public KbQueryResultCollection ExecuteStatementExplain(ulong processId, string statement)
        {
            var results = new KbQueryResultCollection();
            foreach (var preparedQuery in StaticQueryParser.PrepareBatch(statement))
            {
                results.Add(_core.Query.ExplainQuery(processId, preparedQuery));
            }
            return results;
        }

        public KbQueryResultCollection ExecuteStatementProcedure(ulong processId, KbProcedure procedure)
        {
            return _core.Query.ExecuteProcedure(processId, procedure);
        }

        public KbQueryResultCollection ExecuteStatementQuery(ulong processId, string statement)
        {
            var results = new KbQueryResultCollection();
            foreach (var preparedQuery in StaticQueryParser.PrepareBatch(statement))
            {
                results.Add(_core.Query.ExecuteQuery(processId, preparedQuery));
            }
            return results;
        }

        public KbQueryResultCollection ExecuteStatementQueries(ulong processId, List<string> statements)
        {
            var results = new KbQueryResultCollection();

            foreach (var statement in statements)
            {
                foreach (var preparedQuery in StaticQueryParser.PrepareBatch(statement))
                {
                    var intermediatResult = _core.Query.ExecuteQuery(processId, preparedQuery);

                    results.Add(intermediatResult);
                }
            }
            return results;
        }

        public KbActionResponseCollection ExecuteStatementNonQuery(ulong processId, string statement)
        {
            var results = new KbActionResponseCollection();
            foreach (var preparedQuery in StaticQueryParser.PrepareBatch(statement))
            {
                results.Add(_core.Query.ExecuteNonQuery(processId, preparedQuery));
            }
            return results;
        }
    }
}
