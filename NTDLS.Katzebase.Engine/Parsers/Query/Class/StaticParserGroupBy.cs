using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Tokens;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Class
{
    internal static class StaticParserGroupBy
    {
        public static QueryFieldCollection Parse(QueryBatch<TData> queryBatch, Tokenizer tokenizer)
        {
            return StaticParserFieldList.Parse(queryBatch, tokenizer, [" order ", " offset "], true);
        }
    }
}
