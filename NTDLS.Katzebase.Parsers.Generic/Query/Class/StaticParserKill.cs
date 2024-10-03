using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;
using NTDLS.Katzebase.Parsers.Interfaces;

namespace NTDLS.Katzebase.Parsers.Query.Class
{
    public static class StaticParserKill<TData> where TData : IStringable
    {
        internal static PreparedQuery<TData> Parse(QueryBatch<TData> queryBatch, Tokenizer<TData> tokenizer)
        {
            var query = new PreparedQuery<TData>(queryBatch, QueryType.Kill);

            var referencedProcessId = tokenizer.EatGetNextEvaluated<ulong>();
            try
            {
                query.AddAttribute(PreparedQuery<TData>.QueryAttribute.ProcessId, referencedProcessId);
            }
            catch
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Found [{referencedProcessId}], expected: numeric process id.");
            }

            return query;
        }
    }
}
