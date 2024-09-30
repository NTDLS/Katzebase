using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Query.Class
{
    public static class StaticParserKill
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
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Found [{referencedProcessId}], expected: numeric process id.");
            }

            return query;
        }
    }
}
