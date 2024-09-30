using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Parsers.Query.Class.Helpers;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Query.WhereAndJoinConditions;
using NTDLS.Katzebase.Parsers.Tokens;

namespace NTDLS.Katzebase.Parsers.Query.Class
{
    public static class StaticParserWhere
    {
        public static ConditionCollection Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            //Look for tokens that would mean the end of the where clause
            if (tokenizer.TryFindNextIndexOfAny([" group ", " order ", " offset ", " inner "], out var nextPartOfQueryCaret) == false)
            {
                nextPartOfQueryCaret = tokenizer.Length; //Well, I suppose we will take the remainder of the query text.
            }

            //Maybe we end at the next query?
            if (tokenizer.TryFindCompareNext((o) => StaticParserUtility.IsStartOfQuery(o), out var foundToken, out var startOfNextQueryCaret) == false)
            {
                startOfNextQueryCaret = tokenizer.Length; //Well, I suppose we will take the remainder of the query text.
            }

            int?[] carets = [nextPartOfQueryCaret, startOfNextQueryCaret];

            var endOfConditionsCaret = carets.Min().EnsureNotNull();

            string testCondition = tokenizer.SubStringAbsolute(endOfConditionsCaret).Trim();
            if (testCondition == string.Empty)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected conditions, found: [{testCondition}].");
            }

            return StaticConditionsParser.Parse(queryBatch, tokenizer, testCondition, endOfConditionsCaret);
        }
    }
}
