using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;
using NTDLS.Katzebase.Parsers.Interfaces;
namespace NTDLS.Katzebase.Parsers.Query.Class
{
    public static class StaticParserRebuild<TData> where TData : IStringable
    {
        internal static PreparedQuery<TData> Parse(QueryBatch<TData> queryBatch, Tokenizer<TData> tokenizer)
        {
            var querySubType = tokenizer.EatIfNextEnum([SubQueryType.Index, SubQueryType.UniqueKey]);

            return querySubType switch
            {
                SubQueryType.Index => StaticParserRebuildIndex<TData>.Parse(queryBatch, tokenizer),
                SubQueryType.UniqueKey => StaticParserRebuildUniqueKey<TData>.Parse(queryBatch, tokenizer),

                _ => throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"The query type is not implemented: [{querySubType}].")
            };
        }
    }
}

