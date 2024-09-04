using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Query.Constraints;
using NTDLS.Katzebase.Engine.Query.SupportingTypes;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Class.Generic
{
    internal static class StaticWhereParser
    {
        public static Conditions Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            //Look for tokens that would mean the end of the where clause
            if (tokenizer.TryGetNextIndexOf([" group ", " order "], out int endOfWhere) == false)
            {
                //Maybe we end at the next query?
                if (tokenizer.TryGetNextIndexOf((o) => Generic.Helpers.IsStartOfQuery(o), out endOfWhere) == false)
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

            return Conditions.Create(queryBatch, conditionText, tokenizer);
        }
    }
}
