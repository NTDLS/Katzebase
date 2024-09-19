using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Tokens;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Class
{
    internal static class StaticParserGroupBy
    {
        public static QueryFieldCollection Parse(QueryBatch query, Tokenizer tokenizer)
        {
            return StaticParserFieldList.Parse(query, tokenizer, [" order ", " offset "], true);
        }
    }
}
