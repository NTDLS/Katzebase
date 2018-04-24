using System;

namespace Dokdex.Engine.Query
{
    public class QueryManager
    {
        private Core core;

        public QueryManager(Core core)
        {
            this.core = core;
        }

        public void Execute(UInt64 processId, string statement)
        {
            var preparedQuery = ParserEngine.ParseQuery(statement);
            Execute(processId, preparedQuery);
        }

        public void Execute(UInt64 processId, PreparedQuery preparedQuery)
        {
            if (preparedQuery.QueryType == Constants.QueryType.Select)
            {
                core.Documents.ExecuteSelect(processId, preparedQuery);
            }
        }
    }
}
