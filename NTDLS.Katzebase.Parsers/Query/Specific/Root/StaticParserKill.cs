using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Query.Specific.Root
{
    public static class StaticParserKill
    {
        internal static SupportingTypes.Query Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new SupportingTypes.Query(queryBatch, QueryType.Kill, tokenizer.GetCurrentLineNumber());

            var referencedProcessId = tokenizer.EatGetNextResolved<ulong>();
            try
            {
                query.AddAttribute(SupportingTypes.Query.Attribute.ProcessId, referencedProcessId);
            }
            catch
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected numeric process id, found: [{referencedProcessId}].");
            }

            return query;
        }
    }
}
