using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;

namespace NTDLS.Katzebase.Parsers.Query.Class
{
    public static class StaticParserOrderBy
    {
        public static OrderByFieldCollection Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            return StaticParserFieldList.Parse(queryBatch, tokenizer, [], true, (p) => new OrderByFieldCollection(p));
        }
    }
}
