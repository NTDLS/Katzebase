using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Query.Constraints;
using NTDLS.Katzebase.Engine.Query.SupportingTypes;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Class.Generic
{
    internal static class StaticGroupByParser
    {
        public static QueryFieldCollection Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            //Look for tokens that would mean the end of the where clause
            if (tokenizer.TryGetNextIndexOf([" order "], out int endOfWhere) == false)
            {
                //Maybe we end at the next query?
                if (tokenizer.TryGetNextIndexOf((o) => Generic.ParserHelpers.IsStartOfQuery(o), out endOfWhere) == false)
                {
                    //Well, I suppose we will take the remainder of the query text.
                    endOfWhere = tokenizer.Length;
                }
            }

            string groupByFieldList = tokenizer.EatSubStringAbsolute(endOfWhere).Trim();
            if (groupByFieldList == string.Empty)
            {
                throw new KbParserException("Invalid query. Found '" + groupByFieldList + "', expected: list of grouping fields.");
            }

            return null;
        }
    }
}
