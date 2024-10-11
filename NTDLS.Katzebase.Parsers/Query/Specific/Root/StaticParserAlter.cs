using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Query.Specific.Root
{
    public static class StaticParserAlter
    {
        internal static SupportingTypes.Query Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            var querySubType = tokenizer.EatIfNextEnum([SubQueryType.Schema, SubQueryType.Role, SubQueryType.Configuration]);

            return querySubType switch
            {
                SubQueryType.Schema => StaticParserAlterSchema.Parse(queryBatch, tokenizer),
                SubQueryType.Configuration => StaticParserAlterConfiguration.Parse(queryBatch, tokenizer),
                SubQueryType.Role => StaticParserAlterRole.Parse(queryBatch, tokenizer),

                _ => throw new KbNotImplementedException($"The query type is not implemented: [{querySubType}].")
            };
        }
    }
}
