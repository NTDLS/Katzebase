using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Parsers.Query.Class.Helpers;
using NTDLS.Katzebase.Engine.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Engine.Parsers.Tokens;
using NTDLS.Katzebase.Shared;
using static NTDLS.Katzebase.Client.KbConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Class
{
    internal static class StaticParserOrderBy
    {
        public static SortFields Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            string token;
            var sortFields = new SortFields();
            var fields = new List<string>();


            //Look for tokens that would mean the end of the where clause
            if (tokenizer.TryGetNextIndexOfAny([" group ", " offset "], out int endOfWhere) == false)
            {
                //Maybe we end at the next query?
                if (tokenizer.TryEatCompareNext((o) => StaticParserUtility.IsStartOfQuery(o), out endOfWhere) == false)
                {
                    //Well, I suppose we will take the remainder of the query text.
                    endOfWhere = tokenizer.Length;
                }
            }

            string orderByText = tokenizer.EatSubStringAbsolute(endOfWhere).Trim();
            if (orderByText == string.Empty)
            {
                throw new KbParserException("Invalid query. Found '" + orderByText + "', expected: list of conditions.");
            }

            var fieldsTexts = orderByText.ScopeSensitiveSplit(',');

            foreach (var field in fieldsTexts)
            {
                var fieldTokenizer = new TokenizerSlim(field);

                var fieldText = fieldTokenizer.EatGetNext();

                var sortDirection = KbSortDirection.Ascending;
                if (fieldTokenizer.TryEatIsNextToken(["asc", "desc"], out token))
                {
                    sortDirection = token.Is("desc") ? KbSortDirection.Descending : KbSortDirection.Ascending;
                }
                else
                {
                    if (!fieldTokenizer.IsExausted())
                    {
                        throw new KbParserException("Invalid query. Found '" + token + "', expected: asc or desc.");
                    }
                }

                sortFields.Add(fieldText, sortDirection);
            }

            return sortFields;
        }
    }
}
