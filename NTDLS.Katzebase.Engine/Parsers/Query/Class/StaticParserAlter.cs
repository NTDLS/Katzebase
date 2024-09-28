using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Tokens;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Class
{
    internal static class StaticParserAlter<TData> where TData : IStringable
    {
        internal static PreparedQuery<TData> Parse(QueryBatch<TData> queryBatch, Tokenizer<TData> tokenizer)
        {
            var querySubType = tokenizer.EatIfNextEnum([SubQueryType.Schema, SubQueryType.Configuration]);

            return querySubType switch
            {
                SubQueryType.Schema => StaticParserAlterSchema<TData>.Parse(queryBatch, tokenizer),
                SubQueryType.Configuration => StaticParserAlterConfiguration<TData>.Parse(queryBatch, tokenizer),

                _ => throw new KbParserException($"The query type is not implemented: [{querySubType}].")
            };
        }
    }
}
