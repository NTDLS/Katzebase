using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Tokens;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Class
{
    internal static class StaticParserKill
    {
        internal static PreparedQuery Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new PreparedQuery(queryBatch, QueryType.Kill);

            var referencedProcessId = tokenizer.EatGetNextEvaluated<ulong>();
            try
            {
                query.AddAttribute(PreparedQuery.QueryAttribute.ProcessId, referencedProcessId);
            }
            catch
            {
                throw new KbParserException("Invalid query. Found '" + referencedProcessId + "', expected: numeric process id.");
            }

            return query;
        }
    }
}
