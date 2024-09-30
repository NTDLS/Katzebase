using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Parsers.Query.Class.Helpers;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Client.KbConstants;

namespace NTDLS.Katzebase.Parsers.Query.Class
{
    public static class StaticParserOrderBy
    {
        public static SortFields Parse(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            string token;
            var sortFields = new SortFields();
            var fields = new List<string>();

            //Look for tokens that would mean the end of the where clause
            if (tokenizer.TryFindNextIndexOfAny([" group ", " offset "], out var nextPartOfQueryCaret) == false)
            {
                nextPartOfQueryCaret = tokenizer.Length; //Well, I suppose we will take the remainder of the query text.
            }

            //Maybe we end at the next query?
            if (tokenizer.TryFindCompareNext((o) => StaticParserUtility.IsStartOfQuery(o), out var foundToken, out var startOfNextQueryCaret) == false)
            {
                startOfNextQueryCaret = tokenizer.Length; //Well, I suppose we will take the remainder of the query text.
            }

            int?[] carets = [nextPartOfQueryCaret, startOfNextQueryCaret];

            var endOfOrderByCaret = carets.Min().EnsureNotNull();

            string testOrderBy = tokenizer.SubStringAbsolute(endOfOrderByCaret).Trim();
            if (testOrderBy == string.Empty)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Found [{testOrderBy}], expected: list of order by fields.");
            }

            foreach (var field in tokenizer.EatScopeSensitiveSplit(endOfOrderByCaret))
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
                    if (!fieldTokenizer.IsExhausted())
                    {
                        throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Found [{token}], expected: asc or desc.");
                    }
                }

                sortFields.Add(fieldText, sortDirection);
            }

            return sortFields;
        }
    }
}
