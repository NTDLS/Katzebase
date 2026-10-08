using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Parsing.Root
{
    public static class StaticParserAttach
    {
        internal static PreparedQuery Parse(PreparedQueryBatch queryBatch, Tokenizer tokenizer)
        {
            var querySubType = tokenizer.EatIfNextEnum([SubQueryType.Schema]);

            return querySubType switch
            {
                SubQueryType.Schema => StaticParserAttachSchema.Parse(queryBatch, tokenizer),

                _ => throw new KbNotImplementedException($"Query type is not implemented: [{querySubType}].")
            };
        }
    }
}
