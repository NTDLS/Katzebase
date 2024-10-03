using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using NTDLS.Katzebase.Parsers.Interfaces;
namespace NTDLS.Katzebase.Parsers.Query.Class
{
    public static class StaticParserGroupBy<TData> where TData : IStringable
    {
        public static QueryFieldCollection<TData> Parse(QueryBatch<TData> queryBatch, Tokenizer<TData> tokenizer, Func<string, TData> parseStringToDoc, Func<string, TData> castStringToDoc)
        {
            return StaticParserFieldList<TData>.Parse(queryBatch, tokenizer, [" order ", " offset "], true, parseStringToDoc: parseStringToDoc, castStringToDoc: castStringToDoc);
        }
    }
}
