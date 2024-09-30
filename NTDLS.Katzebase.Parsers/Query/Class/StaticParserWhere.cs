using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Query.WhereAndJoinConditions;
using NTDLS.Katzebase.Parsers.Tokens;

namespace NTDLS.Katzebase.Parsers.Query.Class
{
    public static class StaticParserWhere
    {
        public static ConditionCollection Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            var endOfConditionsCaret = tokenizer.FindEndOfQuerySegment([" group ", " order ", " offset ", " inner "]);

            string testCondition = tokenizer.SubStringAbsolute(endOfConditionsCaret).Trim();
            if (testCondition == string.Empty)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected conditions, found: [{testCondition}].");
            }

            return StaticConditionsParser.Parse(queryBatch, tokenizer, testCondition, endOfConditionsCaret);
        }
    }
}
