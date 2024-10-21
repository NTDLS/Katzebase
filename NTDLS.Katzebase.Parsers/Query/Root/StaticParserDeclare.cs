using NTDLS.Katzebase.Parsers.Fields;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Query.Root
{
    public static class StaticParserDeclare
    {
        internal static PreparedQuery Parse(PreparedQueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new PreparedQuery(queryBatch, QueryType.Declare, tokenizer.GetCurrentLineNumber())
            {
                //SubQueryType = SubQueryType.
            };

            var variablePlaceholder = tokenizer.EatGetNext();
            tokenizer.EatIfNext('=');

            var endOfDeclareCaret = tokenizer.FindEndOfQuerySegment();

            if (tokenizer.Variables.Collection.TryGetValue(variablePlaceholder, out var _))
            {
                var expressionText = tokenizer.EatSubStringAbsolute(endOfDeclareCaret).Trim();
                var mockFields = new SelectFieldCollection(query.Batch);
                var expression = StaticParserField.Parse(tokenizer, expressionText, mockFields);

                query.AddAttribute(PreparedQuery.Attribute.VariablePlaceholder, variablePlaceholder);
                query.AddAttribute(PreparedQuery.Attribute.Expression, expression);
            }

            return query;
        }
    }
}
