using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Query.Specific
{
    public static class StaticParserDeclare
    {
        internal static PreparedQuery Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new PreparedQuery(queryBatch, QueryType.Declare, tokenizer.GetCurrentLineNumber())
            {
                //SubQueryType = SubQueryType.
            };

            var variablePlaceholder = tokenizer.EatGetNext();
            tokenizer.EatIfNext('=');

            var endOfDeclareCaret = tokenizer.FindEndOfQuerySegment();

            if (tokenizer.Variables.TryGetValue(variablePlaceholder, out var identifier))
            {
                var expression = tokenizer.EatSubStringAbsolute(endOfDeclareCaret).Trim();

                query.AddAttribute(PreparedQuery.Attribute.VariablePlaceholder, variablePlaceholder);
                query.AddAttribute(PreparedQuery.Attribute.Expression, expression);
            }

            return query;
        }
    }
}
