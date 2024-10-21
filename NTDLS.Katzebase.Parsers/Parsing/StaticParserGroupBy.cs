using NTDLS.Katzebase.Parsers.Fields;
using NTDLS.Katzebase.Parsers.Tokens;

namespace NTDLS.Katzebase.Parsers.Parsing
{
    public static class StaticParserGroupBy
    {
        public static GroupByFieldCollection Parse(PreparedQueryBatch queryBatch, Tokenizer tokenizer)
        {
            return StaticParserFieldList.Parse(queryBatch, tokenizer, [" order ", " offset "], true, (p) => new GroupByFieldCollection(p));
        }
    }
}
