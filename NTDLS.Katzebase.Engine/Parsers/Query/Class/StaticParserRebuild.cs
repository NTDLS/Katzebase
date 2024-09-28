using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Tokens;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Class
{
    internal static class StaticParserRebuild<TData> where TData : IStringable
    {
        internal static PreparedQuery<TData> Parse(QueryBatch<TData> queryBatch, Tokenizer<TData> tokenizer)
        {
            var querySubType = tokenizer.EatIfNextEnum([SubQueryType.Index, SubQueryType.UniqueKey]);

            return querySubType switch
            {
                SubQueryType.Index => StaticParserRebuildIndex<TData>.Parse(queryBatch, tokenizer),
                SubQueryType.UniqueKey => StaticParserRebuildUniqueKey<TData>.Parse(queryBatch, tokenizer),

                _ => throw new KbParserException($"The query type is not implemented: [{querySubType}].")
            };
        }
    }
}

