using NTDLS.Katzebase.Parsers.Fields;
using NTDLS.Katzebase.Parsers.Tokens;

namespace NTDLS.Katzebase.Parsers.Parsing
{
    public static class StaticParserSelectFields
    {
        public static SelectFieldCollection Parse(PreparedQueryBatch queryBatch, Tokenizer tokenizer)
        {
            return StaticParserFieldList.Parse(queryBatch, tokenizer, [" from ", " into "], true, (p) => new SelectFieldCollection(p));
        }
    }
}
