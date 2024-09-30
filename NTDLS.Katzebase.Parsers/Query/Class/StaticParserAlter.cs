using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Query.Class
{
    public static class StaticParserAlter
    {
        internal static PreparedQuery Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            var querySubType = tokenizer.EatIfNextEnum([SubQueryType.Schema, SubQueryType.Configuration]);

            return querySubType switch
            {
                SubQueryType.Schema => StaticParserAlterSchema.Parse(queryBatch, tokenizer),
                SubQueryType.Configuration => StaticParserAlterConfiguration.Parse(queryBatch, tokenizer),

                _ => throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"The query type is not implemented: [{querySubType}].")
            };
        }
    }
}
