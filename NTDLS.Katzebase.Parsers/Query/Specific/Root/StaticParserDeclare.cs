using NTDLS.Katzebase.Parsers.Query.Fields;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Query.Specific.Root
{
    public static class StaticParserDeclare
    {
        internal static SupportingTypes.Query Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            var query = new SupportingTypes.Query(queryBatch, QueryType.Declare, tokenizer.GetCurrentLineNumber())
            {
                //SubQueryType = SubQueryType.
            };

            var variablePlaceholder = tokenizer.EatGetNext();
            tokenizer.EatIfNext('=');

            var endOfDeclareCaret = tokenizer.FindEndOfQuerySegment();

            if (tokenizer.Variables.Collection.TryGetValue(variablePlaceholder, out var identifier))
            {
                var expressionText = tokenizer.EatSubStringAbsolute(endOfDeclareCaret).Trim();
                var mockFields = new SelectFieldCollection(query.Batch);
                var expression = StaticParserField.Parse(tokenizer, expressionText, mockFields);

                query.AddAttribute(SupportingTypes.Query.Attribute.VariablePlaceholder, variablePlaceholder);
                query.AddAttribute(SupportingTypes.Query.Attribute.Expression, expression);
            }

            return query;
        }
    }
}
