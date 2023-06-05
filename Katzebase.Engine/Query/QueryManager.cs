using Katzebase.Engine.KbLib;
using Katzebase.PublicLibrary.Payloads;

namespace Katzebase.Engine.Query
{
    public class QueryManager
    {
        private Core core;

        public QueryManager(Core core)
        {
            this.core = core;
        }

        public KbQueryResult ExecuteQuery(ulong processId, string statement)
        {
            var preparedQuery = ParserEngine.ParseQuery(statement);
            return ExecuteQuery(processId, preparedQuery);
        }

        public KbQueryResult ExecuteQuery(ulong processId, PreparedQuery preparedQuery)
        {
            if (preparedQuery.QueryType == EngineConstants.QueryType.Select)
            {
                return core.Documents.ExecuteSelect(processId, preparedQuery);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public KbActionResponse ExecuteNonQuery(ulong processId, string statement)
        {
            var preparedQuery = ParserEngine.ParseQuery(statement);
            return ExecuteNonQuery(processId, preparedQuery);
        }

        public KbActionResponse ExecuteNonQuery(ulong processId, PreparedQuery preparedQuery)
        {
            if (preparedQuery.QueryType == EngineConstants.QueryType.Select)
            {
                return core.Documents.ExecuteSelect(processId, preparedQuery);
            }
            else if (preparedQuery.QueryType == EngineConstants.QueryType.Delete)
            {
                return core.Documents.ExecuteDelete(processId, preparedQuery);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

    }
}
