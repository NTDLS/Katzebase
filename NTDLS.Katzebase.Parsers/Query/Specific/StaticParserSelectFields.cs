using NTDLS.Katzebase.Parsers.Query.Fields;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;

namespace NTDLS.Katzebase.Parsers.Query.Specific
{
    public static class StaticParserSelectFields
    {
        public static SelectFieldCollection Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            return StaticParserFieldList.Parse(queryBatch, tokenizer, [" from ", " into "], true, (p) => new SelectFieldCollection(p));
        }
    }
}
