using NTDLS.Katzebase.Parsers.Fields;
using NTDLS.Katzebase.Parsers.Tokens;

namespace NTDLS.Katzebase.Parsers.Query
{
    public static class StaticParserOrderBy
    {
        public static OrderByFieldCollection Parse(PreparedQueryBatch queryBatch, Tokenizer tokenizer)
        {
            return StaticParserFieldList.Parse(queryBatch, tokenizer, [" offset "], true, (p) => new OrderByFieldCollection(p));
        }
    }
}
