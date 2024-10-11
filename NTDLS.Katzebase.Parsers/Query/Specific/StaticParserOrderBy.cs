using NTDLS.Katzebase.Parsers.Query.Fields;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;

namespace NTDLS.Katzebase.Parsers.Query.Specific
{
    public static class StaticParserOrderBy
    {
        public static OrderByFieldCollection Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            return StaticParserFieldList.Parse(queryBatch, tokenizer, [" offset "], true, (p) => new OrderByFieldCollection(p));
        }
    }
}
