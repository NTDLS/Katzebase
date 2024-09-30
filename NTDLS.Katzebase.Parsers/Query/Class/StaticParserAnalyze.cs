using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Query.Class
{
    public static class StaticParserAnalyze
    {
        internal static PreparedQuery Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            var querySubType = tokenizer.EatIfNextEnum([SubQueryType.Schema, SubQueryType.Index]);

            return querySubType switch
            {
                SubQueryType.Schema => StaticParserAnalyzeSchema.Parse(queryBatch, tokenizer),
                SubQueryType.Index => StaticParserAnalyzeIndex.Parse(queryBatch, tokenizer),

                _ => throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Query type is not implemented: [{querySubType}].")
            };
        }
    }
}
