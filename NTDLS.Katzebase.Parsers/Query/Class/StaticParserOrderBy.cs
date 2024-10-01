using NTDLS.Helpers;
using NTDLS.Katzebase.Client.Exceptions;
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

            var endOfOrderByCaret = tokenizer.FindEndOfQuerySegment([" group ", " offset "]);
            string testText = tokenizer.SubStringAbsolute(endOfOrderByCaret).Trim();
            if (string.IsNullOrWhiteSpace(testText))
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected ordering expressions, found: [{testText}].");
            }

            try
            {
                tokenizer.PushSyntheticLimit(endOfOrderByCaret);

                string testOrderBy = tokenizer.SubStringAbsolute(endOfOrderByCaret).Trim();
                if (testOrderBy == string.Empty)
                {
                    throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected order by fields, found: [{testOrderBy}].");
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

                    if (!fieldTokenizer.IsExhausted())
                    {
                        throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Expected [asc] or [desc] found: [{tokenizer.ResolveLiteral(token)}].");
                    }

                    sortFields.Add(tokenizer.GetCurrentLineNumber(), fieldText, sortDirection);
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                tokenizer.PopSyntheticLimit();
                tokenizer.EatWhiteSpace();
            }
            return sortFields;
        }
    }
}
