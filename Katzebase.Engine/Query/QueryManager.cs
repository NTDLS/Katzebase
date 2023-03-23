using Katzebase.Library.Payloads;

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
            if (preparedQuery.QueryType == Constants.QueryType.Select)
            {
                return core.Documents.ExecuteSelect(processId, preparedQuery);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
