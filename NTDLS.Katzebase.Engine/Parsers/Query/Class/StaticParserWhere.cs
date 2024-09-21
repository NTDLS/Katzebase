using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Parsers.Query.Class.Helpers;
using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Query.WhereAndJoinConditions;
using NTDLS.Katzebase.Engine.Parsers.Tokens;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Class
{
    internal static class StaticParserWhere
    {
        public static ConditionCollection Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            //Look for tokens that would mean the end of the where clause
            if (tokenizer.TryGetNextIndexOfAny([" group ", " order ", " offset ", " inner "], out int endOfWhere) == false)
            {
                //Maybe we end at the next query?
                if (tokenizer.TryEatCompareNext((o) => StaticParserUtility.IsStartOfQuery(o), out endOfWhere) == false)
                {
                    //Well, I suppose we will take the remainder of the query text.
                    endOfWhere = tokenizer.Length;
                }
            }

            string conditionText = tokenizer.EatSubStringAbsolute(endOfWhere).Trim();
            if (conditionText == string.Empty)
            {
                throw new KbParserException("Invalid query. Found '" + conditionText + "', expected: list of conditions.");
            }

            return StaticConditionsParser.Parse(queryBatch, tokenizer, conditionText);
        }
    }
}
