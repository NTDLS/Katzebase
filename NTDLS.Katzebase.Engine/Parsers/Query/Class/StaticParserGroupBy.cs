using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Tokens;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Class
{
    internal static class StaticParserGroupBy<TData> where TData : IStringable
    {
        public static QueryFieldCollection<TData> Parse(QueryBatch<TData> queryBatch, Tokenizer<TData> tokenizer)
        {
            return StaticParserFieldList<TData>.Parse(queryBatch, tokenizer, [" order ", " offset "], true);
        }
    }
}
