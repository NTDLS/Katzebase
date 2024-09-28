using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Tokens;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Class
{
    internal static class StaticParserList<TData> where TData : IStringable
    {
        internal static PreparedQuery<TData> Parse(QueryBatch<TData> queryBatch, Tokenizer<TData> tokenizer)
        {
            var querySubType = tokenizer.EatIfNextEnum([SubQueryType.Documents, SubQueryType.Schemas]);

            return querySubType switch
            {
                SubQueryType.Documents => StaticParserListDocuments<TData>.Parse(queryBatch, tokenizer),
                SubQueryType.Schemas => StaticParserListSchemas<TData>.Parse(queryBatch, tokenizer),

                _ => throw new KbParserException($"The query type is not implemented: [{querySubType}].")
            };
        }
    }
}
